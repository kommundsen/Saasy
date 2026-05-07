---
id: ADR-0020
type: adr
state: accepted
title: Event Hubs Capture
date: 2026-05-08
deciders: [kim]
---

# ADR-0020 — Event Hubs Capture

## Context

[ADR-0001](ADR-0001-ingestion-stream.md) puts Event Hubs as the primary ingestion path for raw Events from Integrators. [ADR-0010](ADR-0010-period-boundaries-and-final-close.md) makes Final Close an auditable boundary: at Final Close + audit window, "what the Integrator actually sent" must be recoverable independent of how Saasy processed it.

[ADR-0017](ADR-0017-outbox-retention-and-cold-archive.md) covers the *derived* event log (per-context outbox state-change events) but explicitly does not cover the *inbound* raw Event stream — those are different sources of truth with different audit roles:

- **Outbox** = "Saasy decided X happened" (state changes Saasy committed).
- **Capture** = "Integrator told Saasy that Y was observed" (raw Events as received, regardless of whether Saasy accepted, rejected, or processed them).

Iter 07 (Invoices + Webhooks) is the first iteration where Final Close reconciliation against raw inbound is auditable, so the Event Hubs archive must land before that iteration ships.

This ADR settles the use of **Azure Event Hubs Capture** as the archive mechanism: which hubs have Capture enabled, the destination, format, partitioning, retention, GDPR posture, and how operators read the archive when needed.

## Decision

### Enable Capture on every ingest-side Event Hub

- **Production**: Capture is enabled on the ingest Event Hub from day one of Iter 01. The archive accumulates from the first production Event regardless of when Iter 07 ships, so we don't have to reconstruct backfilled audit history.
- **Sandbox**: Capture is enabled on the sandbox ingest hub with the same configuration. Sandbox volume is lower; the cost is negligible and the parity simplifies operator runbooks.
- **Internal Domain Event Bus** ([ADR-0008](ADR-0008-internal-domain-event-bus.md)) Service Bus subscriptions are not captured by this ADR. The internal bus is transport, not source-of-truth; the per-context outbox (covered by [ADR-0017](ADR-0017-outbox-retention-and-cold-archive.md)) is the durable log of derived events.

### Destination — Azure Blob Storage, separate container per environment

- One Storage Account per environment (the same accounts used by [ADR-0017](ADR-0017-outbox-retention-and-cold-archive.md)), one container per environment: `event-capture-sandbox`, `event-capture-prod`.
- **Lifecycle policy mirrors [ADR-0017](ADR-0017-outbox-retention-and-cold-archive.md)**:
  - Days 0–365 from blob creation → **Cool tier**.
  - Day 365+ → **Archive tier**.
  - Delete at **7 years** from blob creation.
- Server-side encryption with Microsoft-managed keys. Default LRS replication. Customer-managed keys deferred unless regulatory pressure surfaces.

### Format — Avro

Capture writes Avro; this is the only format Capture supports and is not negotiated. The Avro schema embeds the Event Hubs envelope (`SequenceNumber`, `Offset`, `EnqueuedTimeUtc`, `PartitionKey`, system properties, application properties, body bytes). The body is the raw Integrator payload as received.

A reader sketch (`avro-tools` or `pyarrow`) is included in the operator runbook so an audit query is `gunzip-equivalent` plus one tool. No bespoke reader infrastructure.

### Path template — Capture default

```
{Namespace}/{EventHub}/{PartitionId}/{Year}/{Month}/{Day}/{Hour}/{Minute}/{Second}
```

- This is Event Hubs Capture's default template. Capture doesn't read payload contents and cannot partition by `integrator_id` — the integrator slug is a body field, not a system property. Per-Integrator filtering is a downstream operation against the Avro files (see GDPR + audit access below).
- The time fields are based on the **window-start time**, not the Event's `EnqueuedTimeUtc`. A window emits one blob per partition; within a blob, Events are ordered by Event Hubs sequence within that partition.

### Window — 5 minutes / 300 MB, whichever first

- **Time window**: 5 minutes (Capture's default).
- **Size window**: 300 MB (Capture's default).
- These are the documented defaults. Tuning rationale: at projected v1 volumes, the time window dominates (most blobs hit 5 minutes well before 300 MB), so latency-to-evidence is bounded at ~5 minutes. Smaller windows would multiply blob count without proportionate audit benefit; larger windows would extend the worst-case "we don't have a blob yet" window past what feels right for incident triage.
- **Empty windows**: Capture's "Emit empty files" toggle is **disabled**. Audit queries treat the absence of a blob for a window as "no Events that window," which is correct.

### Retention horizon — 7 years from `EnqueuedTimeUtc`

- The lifecycle policy deletes blobs at 7 years from blob creation, which tracks `EnqueuedTimeUtc` to within the window-size delay (≤5 min).
- Aligned with [ADR-0017](ADR-0017-outbox-retention-and-cold-archive.md). Specific Integrators with longer retention obligations are handled by export-to-Integrator-controlled-storage (a runbook procedure, not Saasy's retention).

### GDPR / right-to-erasure — operator-driven scan-and-rewrite

The path template doesn't partition by `integrator_id`, so erasure cannot be a bounded blob-delete. The accepted posture:

- A whole-Integrator GDPR purge runs as a **one-shot Container App Job** that:
  1. Lists every blob in `event-capture-<env>/...` whose window overlaps the Integrator's lifetime.
  2. For each blob: reads the Avro, filters out records matching the target `integrator_id`, writes the filtered Avro back to the same path with a new ETag, and deletes the original.
  3. Records the operation in the audit log with row counts before/after per blob.
- The job is slow (proportional to Integrator activity volume × archive horizon) but bounded and resumable. It is run rarely (whole-Integrator deletions are not routine).
- **Customer-level GDPR erasure does not reach Capture**, on the same legitimate-interest grounds documented in [ADR-0017](ADR-0017-outbox-retention-and-cold-archive.md): the raw Event stream is the audit record of what an Integrator reported about its Customers; redacting it would compromise the audit chain that Final Close depends on.

A continuously-running re-keying pipeline that re-partitions Capture output by Integrator was considered and **rejected**: the pipeline would run constantly to satisfy a workflow that runs annually, and would couple Saasy to a derived archive whose freshness needs to be monitored.

### Audit access — operator-only read job

There is no public or Integrator-facing API for reading Capture. The operator path:

- Internal endpoint `POST /internal/capture/query` accepts `(integrator_id, time_range)`, scopes to the operator's identity, audit-logs the request, and triggers a read job that:
  1. Identifies the overlapping Capture blobs.
  2. Issues `Set Blob Tier` to `Hot` for any Archive-tier blobs (rehydration is async, hours-scale).
  3. On completion, streams the matching Avro records to a temporary read-only location (a SAS-signed blob in an `audit-results` container with 7-day TTL) for the operator to download.
- Reading directly from Cool tier is allowed and fast; the rehydration step is only triggered when the time range crosses into Archive tier (>12 months old).
- Capture blobs are **never modified** by audit reads. The output is a derived artifact in a separate container.

### Reconciliation against Postgres ingest

The audit-against-Final-Close reconciliation pattern:

- For a target Integrator + Billing Period, the operator job reads Capture for the period and counts Events grouped by Customer + Dimension.
- The same counts are read from the Postgres ingest table (the derived store).
- A diff above tolerance (default 0; non-zero diffs are an alert) is recorded in the runbook as an evidence-of-divergence and gates Final Close from completing.

This is documented in `docs/runbooks/final-close-reconciliation.md`. The runbook itself is iter-07 work; this ADR commits to the data-plane shape that makes the runbook implementable.

## Consequences

### Wins

- **Capture is configured infrastructure, not application code.** Saasy services don't run an Event-archive consumer; Azure does the work, and outages in Saasy's services don't lose audit data.
- **The archive predates audit need.** Capture turns on at Iter 01; by the time Iter 07's Final Close reconciliation arrives, there's a year of pre-existing audit data, not a cutover.
- **Lifecycle alignment with [ADR-0017](ADR-0017-outbox-retention-and-cold-archive.md)** keeps two archives operating on identical rules — one set of operational knobs, one cost model, one runbook for tier-rehydration.
- **No re-keying pipeline** removes a continuously-running piece of infrastructure whose only customer is a rare operation.
- **Avro is well-supported by audit tooling.** `avro-tools`, `pyarrow`, `kafkacat` (with the right plugin) all read it directly.
- **Reconciliation has a concrete data-plane** — Iter 07's runbook can be written against this ADR without further infrastructure decisions.

### Costs

- **Capture has its own per-namespace cost** on top of throughput units. Bounded and predictable, but a non-zero line item starting at Iter 01.
- **Per-Integrator GDPR purge is slow and operator-driven.** The Avro scan-and-rewrite job is bounded but expensive in operator time. Rare enough that this is acceptable; not zero-cost when it happens.
- **Capture's path template is rigid.** The default partitioning by time + partition-id is what we get; no per-Integrator partitioning at write time. Downstream queries scan more blobs than they would with a custom-partitioned archive.
- **Window size dictates latency-to-evidence.** A 5-minute window means a freshly-emitted Event isn't audit-queryable for up to 5 minutes. Acceptable for the audit use cases in scope; would not be acceptable for real-time forensic use cases (which Saasy doesn't have).
- **Archive-tier rehydration is hours-scale.** Audit queries against >12-month-old data have a meaningful first-step latency. Mirrors [ADR-0017](ADR-0017-outbox-retention-and-cold-archive.md); same tradeoff for the same reasons.
- **Capture and the Postgres ingest table are independent sources.** A bug in either drifts independently; the reconciliation runbook is the safety net but it runs at Final Close, not continuously.

### Out of scope

- **Event Hubs Capture for the internal Domain Event Bus.** The bus is transport; the outbox per [ADR-0017](ADR-0017-outbox-retention-and-cold-archive.md) is the system of record.
- **A continuously-running re-keying pipeline** that re-partitions Capture by Integrator. Rejected explicitly.
- **Real-time audit search** (e.g. live query API for Capture). The operator-only batch read is sufficient for the audit use cases in scope.
- **Capture in a region different from the Event Hubs Namespace.** Capture writes to the same region as the namespace; cross-region archive replication is a Storage Account replication concern, deferred.
- **Customer-managed encryption keys** for the Capture Storage container. Microsoft-managed keys, mirroring [ADR-0017](ADR-0017-outbox-retention-and-cold-archive.md), [ADR-0018](ADR-0018-observability-sinks.md), [ADR-0019](ADR-0019-deployment-topology.md).
- **Cross-Integrator audit views.** Capture archive data crosses Integrators (the partitioning isn't per-Integrator), but operator queries scope to a single Integrator at a time.
