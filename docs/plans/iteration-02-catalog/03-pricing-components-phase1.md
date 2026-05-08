---
title: Pricing Components — Flat Fee, Per-Seat Fee, flat-rate Metered Charge
iteration: 02
status: todo
labels: [catalog, pricing, domain]
depends-on: [02-plan-planversion, 01-aggregation-strategies]
agent: backend
---

# Pricing Components (Phase 1)

Implement three Pricing Components as polymorphic value objects on PlanVersion:

- **FlatFee** — fixed `Money` per Billing Period.
- **PerSeatFee** — `Money` per seat × time-weighted seat count from a `time_weighted_last` Dimension ([ADR-0011](../../decisions/ADR-0011-per-seat-fee-time-weighted-aggregation.md)).
- **MeteredCharge (flat-rate)** — `Money` per unit × aggregated value of a Dimension. Quota / Overage deferred to Iteration 05.

## Acceptance criteria

- Sealed class hierarchy or discriminated union; `PricingComponentKind` discriminator persisted.
- Each component validates referenced Dimension exists in the Plan's ProductType.
- `PerSeatFee` rejects Dimensions whose Aggregation is not `TimeWeightedLast`.
- Component carries an immutable display label (used by Invoice line items in Iteration 07).
- Unit tests cover construction validation per component.
