---
title: Per-context Outbox + Outbox Relay worker
iteration: 03
status: todo
labels: [infrastructure, eventing]
depends-on: []
agent: backend
---

# Per-context Outbox + Outbox Relay worker

Each emitting context owns an `outbox_messages` table in its own schema. The application writes Domain Events to the outbox in the same EF Core transaction that mutates aggregates. A `OutboxRelay` BackgroundService dispatches them to the Internal Domain Event Bus per [ADR-0008](../../decisions/ADR-0008-internal-domain-event-bus.md).

## Acceptance criteria

- Generic `IOutboxWriter` abstraction; per-context migration creates the table.
- `OutboxRelay<TContext>` BackgroundService polls + dispatches in batches with bounded concurrency.
- At-least-once dispatch with Service Bus duplicate detection.
- Per-context relay (Tenancy, Catalog, Subscriptions, Metering) registered in `Saasy.Worker`.
- Telemetry: per-context dispatch lag gauge.
- Dispatches in `outbox.id` order; single active dispatcher per `(context, integrator_id)` partition (leader-election or work-stealing if multi-instance) — required by ADR-0014 ordering rules.
- Outbox row carries `committed_at` propagated through the bus envelope, recorded by consumers for end-to-end lag telemetry.
- **Replay support**: per-context operator-only endpoint `POST /ops/{context}/replay` accepting `(consumer_name, from_outbox_id, to_outbox_id)`; re-dispatches the range without modifying outbox rows. Audit-logged. Required by ADR-0014 reconciliation rules.
