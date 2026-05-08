---
title: Subscription aggregate + state machine
iteration: 03
status: todo
labels: [subscriptions, domain]
depends-on: []
agent: backend
---

# Subscription aggregate

`Subscription` is an Aggregate Root in the Subscriptions context. Holds `CustomerId`, `PlanId`, `PlanVersionId`, `StartAt`, `Status`, `IntegratorId`. Enforces state machine. Plan Transitions are **not** in scope here (Iteration 06).

## Acceptance criteria

- Strongly-Typed `SubscriptionId`.
- Factory `Subscription.Create(integratorId, customerId, planId, planVersionId, startAt)` validates referenced Customer + Plan exist in projections.
- Methods: `Activate()`, `Pause(reason)`, `Resume()`, `Cancel(effectiveAt)`. Each emits a Domain Event.
- Invariant: `Cancelled` is terminal.
- `IsExclusivePerCustomer` enforcement: when the new Subscription's Plan has the flag, reject if any Active/Paused Subscription for the same Customer references a Plan with the same `ProductTypeId` and either Plan has the flag set ([ADR-0015](../../decisions/ADR-0015-customer-subscription-cardinality.md)).
- Persistence under `subscriptions` schema.
