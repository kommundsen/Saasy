# Iteration 03 — Subscriptions & Rollups

## Goal

Bind Customers to PlanVersions via `Subscription` and start producing `Rollup`s from the raw event stream. Establish the per-context Outbox + Service Bus topics + projection pattern that all later iterations will reuse.

## Out of scope

No Quota/Overage (Iteration 05). No Plan Transitions or Proration (Iteration 06). No Final Close yet — Rollups are "running" period values without invoice-time semantics.

## Exit criteria

- `Subscription` aggregate enforces state machine `Draft → Active → Paused → Active`, `Active|Paused → Cancelled` (terminal).
- Subscription references `CustomerId` + `PlanId` + `PlanVersionId` (via Strongly-Typed IDs only); no navigation properties cross context.
- `Rollup` aggregate persisted per `(SubscriptionId, DimensionId, Period)` with current `AggregatedValue`.
- Per-context Outbox table + Outbox Relay worker per [ADR-0008](../../decisions/ADR-0008-internal-domain-event-bus.md).
- Service Bus topics provisioned: `tenancy.events`, `catalog.events`, `subscriptions.events`, `metering.events`. Sessions = `integrator_id`.
- Subscriptions context maintains projections from Tenancy (Customer) + Catalog (Plan, PlanVersion, Dimension) per [ADR-0016](../../decisions/ADR-0016-event-driven-projections.md).
- Property tests for Rollup aggregation correctness across out-of-order + duplicate events.
- ADR-0014 (cross-context consistency model) written and accepted before this iteration ships.

## Issues

- [ ] [00 — Subscription aggregate + state machine](00-subscription-aggregate.md)
- [ ] [01 — Per-context Outbox + Outbox Relay worker](01-outbox-relay.md)
- [ ] [02 — Service Bus topics + sessioned consumers](02-service-bus-topology.md)
- [ ] [03 — Cross-context projections for Subscriptions context](03-projections-subscriptions.md)
- [ ] [04 — Rollup aggregate + Rollup Updater worker](04-rollup-aggregate-worker.md)
- [ ] [05 — Subscription API surface](05-subscription-api.md)
- [ ] [06 — Property tests for Rollup correctness (ordering, duplicates, late arrivals)](06-pbt-rollup-correctness.md)
- [ ] [07 — ADR-0014: cross-context consistency model](07-adr-0014-consistency.md)
