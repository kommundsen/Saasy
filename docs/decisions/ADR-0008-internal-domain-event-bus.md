---
id: ADR-0008
type: adr
state: accepted
title: Per-context outbox + Internal Domain Event Bus on Azure Service Bus
date: 2026-05-06
deciders: [kim]
---

# ADR-0008 — Per-context outbox + Internal Domain Event Bus

## Context

Saasy's outbound deliveries (`usage.threshold.crossed`, `invoice.generated`, …) are critical-must-not-be-lost: a missed Webhook is a missed Threshold alert or a missed Invoice for the Integrator. Saasy adopts the transactional outbox pattern (atomic write of an outbox row alongside the aggregate change) plus an internal bus to decouple emitting contexts from delivery channels.

Two structural constraints:

1. **No cross-context schema reach.** A single shared outbox table would force every context's `DomainEventToOutboxInterceptor` to write across schemas (or databases) inside its own `SaveChangesAsync`, breaking bounded-context isolation. Each emitting context must own its own outbox table.
2. **Target fan-out cannot live in the interceptor.** Looking up `WebhookSubscription` matches inside the emitting context's interceptor would require reading Delivery-context tables (same coupling, in reverse), and would freeze fan-out at write time so a `WebhookSubscription` created between raise and dispatch would miss the event. Fan-out moves to the Dispatcher.

A bus also makes adding new outbound channels (post-v1: external Service Bus / Event Hub delivery to Integrator-owned namespaces) cheap — a new Dispatcher subscribes to the same topics; emitting contexts are unaware.

## Decision

### Per-context outbox

Each emitting context (Tenancy, Catalog, Subscriptions, Metering, Invoicing) owns its own `outbox_messages` table in its own schema, written only by its own `DbContext`:

```sql
CREATE TABLE {context}.outbox_messages (
    id              uuid        PRIMARY KEY,
    integrator_id   uuid        NOT NULL,
    event_kind      text        NOT NULL,
    payload_json    jsonb       NOT NULL,
    occurred_at     timestamptz NOT NULL,
    relayed_at      timestamptz NULL
);
```

`DomainEventToOutboxInterceptor` runs at `SavingChangesAsync` (pre-commit), serializes each Domain Event into integration shape, and inserts rows into THIS context's table in the same `SaveChanges` call. The interceptor never reads any other context's tables. The outbox table is **infrastructure**, not a domain aggregate.

### Internal Domain Event Bus

**Azure Service Bus**, one topic per emitting context: `saasy.domain-events.{tenancy|catalog|subscriptions|metering|invoicing}`.

- **Sessions enabled, session ID = `integrator_id`** — per-Integrator FIFO across that Integrator's events from one context. Cross-Integrator messages process in parallel.
- **Topic-level duplicate detection** (~10 min window). Bus message MessageId = `outbox_messages.id`. Catches republishes from a relay that crashed between bus-publish and `relayed_at` mark.
- Service Bus is chosen over Event Hubs because the workload here — discrete events with rich delivery semantics, per-target retry, dead-lettering — is the textbook Service Bus shape. The internal bus is distinct from any future external Service Bus delivery channel.

### Outbox Relay (per emitting context)

A `BackgroundService` per emitting context, derived from a shared `OutboxRelayBase<TSource>` in `Saasy.Outbox.Infrastructure`. Loop: read unrelayed rows ordered by `occurred_at`, publish each with MessageId = row id and SessionId = `integrator_id`, set `relayed_at` on success. Per-context registration gives failure isolation and independent observability. The shared `ServiceBusClient` is a singleton.

### Channel Dispatchers

v1 ships **`Saasy.Workers.WebhookDispatcher`**. Future channels add sibling worker hosts. The Dispatcher subscribes to every `saasy.domain-events.*` topic with sessions. On each message:

1. Look up active `WebhookSubscription`s for the message's `IntegratorId` whose `SubscribedKinds` include `event_kind`.
2. For each match, `INSERT INTO delivery.outbox_dispatch (...) ON CONFLICT DO NOTHING` — idempotent on bus redelivery.
3. Complete the bus message.

A second loop processes unprocessed dispatch rows: claim, sign with the WebhookSubscription's secret, POST, mark `processed_at` / bump `attempt_count` / set `dead_lettered_at` after max attempts.

```sql
CREATE TABLE delivery.outbox_dispatch (
    id                  uuid        PRIMARY KEY,
    outbox_message_id   uuid        NOT NULL,
    channel             text        NOT NULL,
    target_id           text        NOT NULL,
    claimed_at          timestamptz NULL,
    attempt_count       int         NOT NULL DEFAULT 0,
    last_attempt_at     timestamptz NULL,
    last_error          text        NULL,
    available_at        timestamptz NULL,
    processed_at        timestamptz NULL,
    dead_lettered_at    timestamptz NULL,
    UNIQUE (outbox_message_id, channel, target_id)
);
```

### Bounded context name

The outbound-integration context is named **Delivery** (not "Webhooks") to reflect that v1 ships one channel (Webhook) but future channels add sibling target aggregates (`ServiceBusEndpoint`, `EventHubEndpoint`).

## Consequences

### Wins

- **Bounded-context isolation preserved at the persistence layer.** Each context's interceptor writes only to its own schema.
- **New channels do not touch any emitting context** — a new Dispatcher subscribes to the same topics.
- **Late-created WebhookSubscriptions receive in-flight events** — fan-out is at dispatch time, not write time.
- **Per-channel retry / dead-letter state is independent.**
- **Failure isolation per emitting context** — the Metering relay can be paused without affecting Invoicing.

### Costs

- **One Service Bus namespace per region** (~$10/mo Standard at v1 volume; sessions and duplicate detection are included).
- **At-least-once at three boundaries.** (1) Outbox row → bus (caught by duplicate detection on MessageId), (2) bus → dispatch row insert (caught by the unique key on `outbox_dispatch`), (3) dispatch row → outbound HTTP (Integrators must be idempotent on `event_id`).
- **No global cross-context FIFO.** Per-context, per-Integrator FIFO is preserved. Webhook payloads carry `occurred_at`; Integrators relying on cross-context ordering must order by that field.
- **`Saasy.Outbox.Infrastructure` shared library** houses `IOutboxSource`, `OutboxRelayBase<TSource>`, bus publish helpers, and Service Bus client registration.
- **OutboxRetention worker** prunes a row only when every live channel has terminal state for it AND it's older than the retention window. Adding a channel requires a "starts at cursor=now" registration so it does not fan out historical events.

### Out of scope

Production deployment topology (see [ADR-0019](ADR-0019-deployment-topology.md)); external-channel target aggregates; alternative bus implementations (Kafka, NATS) — the relay → bus → dispatcher shape is portable.
