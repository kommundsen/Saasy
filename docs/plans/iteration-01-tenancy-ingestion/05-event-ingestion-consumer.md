---
title: Event ingestion consumer (raw append-only store)
iteration: 01
status: done
labels: [ingestion, metering, worker]
depends-on: [04-event-hubs-provisioning]
agent: backend
---

# Event ingestion consumer

`Saasy.Worker` hosts a BackgroundService that reads from the `events` hub via `EventProcessorClient`, validates the envelope, and writes to a raw append-only `events` table in the Metering schema. No semantic validation (Dimension lookup) yet.

## Acceptance criteria

- Checkpointing via Azure Blob Storage (emulator locally).
- Envelope schema: `integrator_id`, `customer_external_ref`, `event_type`, `dimension_code`, `value`, `occurred_at`, `idempotency_key`, `payload` (JSONB).
- Duplicate `idempotency_key` per Integrator dropped silently (logged at debug).
- At-least-once with idempotency = effective once.
- Property test: arbitrary partition orderings produce the same set of stored events.
