---
id: ADR-0001
type: adr
state: accepted
title: Azure Event Hubs as the hosted ingestion stream
date: 2026-05-03
deciders: [kim]
---

# ADR-0001 — Azure Event Hubs as the hosted ingestion stream

## Context

Saasy needs a hosted streaming bus for usage Events (PRD §4.2): small append-only records, partition-per-Integrator, sustained 1k events/sec/Integrator with 10k bursts. Service Bus Standard hits per-entity throughput ceilings at this load and is the wrong abstraction for streams; Premium is expensive at low volume; self-managed Kafka is operational overhead Saasy doesn't need.

Usage-metering ingestion is the textbook Event Hubs use case (Stripe-style metering, IoT, App Insights).

## Decision

Use **Azure Event Hubs** as the hosted ingestion stream.

- One namespace per region. Production and sandbox Integrators share the namespace; isolation is per-Integrator at the partition + credential level (per [ADR-0009](ADR-0009-integrator-per-kind-environment-model.md)).
- One Event Hub per namespace; partition count sized for sustained throughput, added as we grow.
- Partition key = `integrator_id` — guarantees per-Integrator ordering and read isolation.
- Per-Integrator credentials are SAS tokens scoped to publish-only.
- Rollup workers consume via partition-aware checkpointing.
- Integrators publish using native Event Hubs SDKs OR the Kafka protocol (Event Hubs Kafka surface).

## Consequences

- **Cost.** ~$50/mo per region at v1 design-partner volume (2 TUs); scales linearly.
- **Lock-in.** Tied to Azure for the bus. Mitigated by Kafka-protocol compatibility — Integrators publishing via Kafka clients require zero changes if Saasy ever migrates.
- **No native DLQ.** Poison Events tracked at the application layer (a `poison_events` table, alerted on).
- **Per-partition ordering only.** Global FIFO is unnecessary; per-Integrator FIFO is what matters.
- **Retention.** Default 7 days, expandable to 90. Long-term archive via Event Hubs Capture → Blob (see [ADR-0020](ADR-0020-event-hubs-capture.md)).
