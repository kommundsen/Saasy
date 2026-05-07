---
id: ADR-0017
type: adr
state: accepted
title: Outbox retention & cold archive
date: 2026-05-08
deciders: [kim]
---

# ADR-0017 — Outbox retention & cold archive

## Context

[ADR-0014](ADR-0014-cross-context-write-and-consistency-model.md) §reconciliation/repair establishes the per-context outbox as the durable log that backs replay tooling. It commits to "≥90 days hot in Postgres, older entries archive to blob storage (cold) and prune" but defers the operational shape: archive store, file format, partitioning, total retention horizon, and replay path from cold. This ADR settles those.

The framing question — *is cold archive a routinely-queryable secondary store, or compliance-grade evidence we hope never to touch?* — is settled as **evidence-only**. Common operational cases (consumer bug, lost event, projection rebuild from scratch on a new context) are absorbed by the 90-day hot window. Replay from cold is a rare, operator-driven recovery, not a query path.

## Decision

### Hot window — 90 days in per-context Postgres outbox tables

- Each Bounded Context owns one `outbox` table. Rows live in Postgres for **90 days** from `committed_at`.
- Hot window is sized to cover: the longest realistic detect-and-fix latency for a consumer bug, a quarter-end projection rebuild, and a Final Close audit window for the longest Billing Period (per [ADR-0010](ADR-0010-period-boundaries-and-final-close.md)) plus headroom.
- The hot window is **not** a function of disk pressure. If a context's outbox volume threatens Postgres I/O, the response is partition the table or scale the database, not shrink the window.

### Cold store — Azure Blob Storage, lifecycle-managed

- One Storage Account per environment (sandbox / production), one container per Bounded Context: `outbox-archive-<context>`.
- **Tiering is automated by Azure Blob lifecycle policy**:
  - Days 0–365 from blob creation → **Cool tier** (immediate read, low storage cost).
  - Day 365+ → **Archive tier** (rehydration required to read, lowest storage cost).
- Server-side encryption with Microsoft-managed keys. Customer-managed keys (CMK) are deferred unless regulatory pressure surfaces; the lifecycle policy and partitioning scheme work identically with CMK and don't need to be re-litigated.
- **No replication beyond the Storage Account default** (LRS). The outbox is a derived log of state already in Postgres + the live system; ZRS/GRS for the cold archive doesn't add proportionate value over Postgres backups.

### Format — gzipped JSONL

- One blob per `(context, date, integrator_id)` partition key.
- Newline-delimited JSON, gzip compressed. One outbox row per line, full envelope (headers + payload) preserved verbatim from the Postgres row.
- **No Parquet, no Avro.** This is evidence; columnar formats optimize for query patterns we explicitly aren't building. Plain JSONL is `gunzip | jq`-able by an operator with no tooling beyond standard CLI.
- Each blob is paired with a sidecar `.manifest.json` containing: row count, min/max `outbox.id`, min/max `committed_at`, SHA-256 of the gzipped data blob, archiving worker identity, and archive timestamp. The manifest is what the prune step reads before deleting Postgres rows.

### Partitioning — `(context, date, integrator_id)`

Blob path layout:

```
outbox-archive-<context>/
  date=YYYY-MM-DD/
    integrator=<integrator_id>.jsonl.gz
    integrator=<integrator_id>.manifest.json
```

- **Date** is the partition key for lifecycle transitions and bulk prune. `date` is the date of `committed_at` in UTC, not archive-write date, so a blob's age tracks the data's age.
- **Integrator** is the partition key for GDPR-scoped operations and per-Integrator replay. One blob per Integrator per day per context.
- **No further sub-partitioning** (no hour, no aggregate-id). Daily granularity is enough; finer partitioning multiplies blob count without proportionate operational benefit.

### Cadence — daily batch

- One scheduled job per context (`OutboxArchiver`) runs at **02:00 UTC daily**.
- The job archives all outbox rows where `committed_at < now() - interval '90 days'` and `committed_at >= last archived watermark`. Watermark is per-context, persisted in a small `outbox_archive_state` table.
- **Write-then-prune, never the reverse.** For each `(context, date, integrator)` partition: write the blob + manifest, fsync, verify the SHA-256 against the manifest, then `DELETE` the corresponding Postgres rows in the same transaction that advances the watermark. A crash between write and prune leaves duplicate data; re-running the archiver is idempotent because partitions are deterministically keyed.
- Failure to archive halts the prune step for that partition and pages on the second consecutive failure. The hot window can grow past 90 days indefinitely while archiving is broken — the system is happier with a fat outbox than with lost evidence.

### Total retention — 7 years from `committed_at`

- A second lifecycle policy deletes blobs at **`committed_at` + 7 years** (i.e. 6 years past archive transition to Archive tier).
- 7 years is the operating baseline for accounting evidence in the jurisdictions Saasy initially targets. Specific regulated Integrators with longer retention obligations are handled by export-to-Integrator-controlled-storage, not by extending Saasy's own retention.
- Retention extension is a configuration knob on the lifecycle policy, not code. Compliance changes don't ship as code.

### Replay from cold — rehydrate, then use ADR-0014 tooling

There is no "replay directly from cold" path. The flow:

1. Operator identifies the time/integrator range needed.
2. Operator-only endpoint `POST /internal/outbox/{context}/rehydrate` accepts `(integrator_id, date_range)` and:
   - Issues `Set Blob Tier` to `Hot` for any blobs currently in Archive tier (rehydration is async, hours-scale).
   - On rehydration completion, streams the blobs back into a **temporary `outbox_rehydrated` table** in the context's database (separate from the live `outbox` table; never merged).
3. Operator runs the existing replay endpoint from [ADR-0014](ADR-0014-cross-context-write-and-consistency-model.md) §reconciliation/repair pointed at `outbox_rehydrated` instead of `outbox`.
4. After replay, the rehydrated table is dropped. Cold blobs are not modified — the cold store remains the system of record.

This is deliberately friction-laden. Cold replay is the "we have a real incident" path; making it ergonomic would invite use as a query path, which the format isn't optimized for.

### GDPR / right-to-erasure

- Soft delete ([ADR-0014](ADR-0014-cross-context-write-and-consistency-model.md)) keeps `DeletedAt` on aggregates; the outbox stream of `*.deleted` events is part of the audit log and is **retained**, not redacted, under legitimate-interest grounds for financial-record-keeping.
- For an erasure-mandated Integrator-level purge (entire Integrator deletion, not a single Customer's GDPR request): a one-off operator job removes all Postgres outbox rows for that `integrator_id` across contexts and deletes the `integrator=<id>.jsonl.gz` blobs in every `outbox-archive-<context>/date=*/` partition. The integrator-keyed partitioning makes this a `Delete Blob` per (context × day) — bounded, scriptable, audit-logged.
- Customer-level GDPR requests within a still-active Integrator are handled at the projection / API layer, not by mutating the outbox. The outbox preserves the historical event sequence; the customer-facing surfaces serve the redacted view.

## Consequences

### Wins

- **The 90-day hot window is sized for the cases that matter** (consumer bugs, projection rebuilds, Final Close audit) without leaning on cold storage for routine work.
- **Lifecycle policy does the tiering** — no application code involved in promoting Cool → Archive or expiring at 7 years. Compliance changes are config, not deploys.
- **Integrator-keyed partitioning** makes whole-Integrator GDPR purge a bounded blob-delete operation.
- **JSONL is grep-able and `jq`-able** — operator UX during a rare cold-replay incident isn't gated on bespoke tooling.
- **Write-then-prune is durably idempotent**, and failure halts the hot prune rather than risking evidence loss.
- **Cold replay is intentionally friction-laden** so the format choice (JSONL, not Parquet) doesn't get re-litigated by query-path requirements.

### Costs

- **JSONL is space-inefficient vs Parquet** — gzipped envelope JSON probably 3–5× the size of a Parquet equivalent. Acceptable at evidence volumes; would not be acceptable as a primary read store.
- **Archive-tier rehydration is hours-scale**, so cold replay against >12-month-old data has a meaningful first-step latency. This is a feature: it forces operators to scope the request.
- **Per-Integrator-per-day blob count grows linearly with active Integrators.** At 10k active Integrators × 365 days × N contexts, blob counts run into the millions over multi-year horizons. Within Azure Blob's per-container limits but worth tracking; a future ADR may collapse partitioning if blob count becomes the operational bottleneck.
- **GDPR purge is per-Integrator only.** Customer-level erasure can't reach the outbox; the legitimate-interest justification for retaining the event stream needs to hold up to legal review per jurisdiction Saasy enters.
- **Manifest verification on every write** adds a small fixed cost per partition; negligible at the daily-batch cadence.

### Out of scope

- **Real-time streaming archive** (e.g. Event Hubs Capture from a relayed copy of the outbox). The daily batch is sufficient at projected volumes; streaming is a future ADR if the hot window becomes a Postgres-volume problem.
- **Customer-managed encryption keys (CMK).** Microsoft-managed keys until a regulated Integrator drives the requirement.
- **Cross-region replication** of the cold archive. Azure Storage default LRS is sufficient given Postgres backups already cover the source-of-truth.
- **Selective field redaction** in archived blobs (e.g. PII scrubbing pre-archive). The archive preserves the event verbatim; redaction is a projection-layer concern.
