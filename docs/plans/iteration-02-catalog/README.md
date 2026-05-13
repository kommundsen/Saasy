# Iteration 02 — Catalog

## Goal

Define what an Integrator can sell: `ProductType`, `Dimension` (with the five Aggregations), `Plan`, and immutable `PlanVersion` carrying `PricingComponent`s. Phase-1 Pricing Components only: Flat Fee, Per-Seat Fee, flat-rate Metered Charge.

## Out of scope

No Subscriptions yet (Iteration 03). No Quota/Overage on Metered Charge (Iteration 05). No Tiered pricing (Iteration 08). No Plan Transitions (Iteration 06).

## Exit criteria

- `ProductType` and `Dimension` aggregates created via Api; Aggregation values constrained to `sum`, `last`, `max`, `unique-count`, `time_weighted_last`.
- `Plan` and `PlanVersion` are separate Aggregate Roots; new edits create a new `PlanVersionId` (immutable history) — see [docs/architecture/aggregates.md](../../architecture/aggregates.md).
- `PricingComponent` value objects implemented for Flat Fee, Per-Seat Fee, flat-rate Metered Charge.
- Per-Seat Fee references a Dimension whose Aggregation is `time_weighted_last` ([ADR-0011](../../decisions/ADR-0011-per-seat-fee-time-weighted-aggregation.md)).
- Property tests for `time_weighted_last` aggregation correctness on synthetic event streams.
- Read-only API: `GET /v1/product-types`, `GET /v1/plans`, `GET /v1/plans/{id}/versions/{versionId}`.

## Issues

- [x] [00 — ProductType + Dimension aggregates](00-product-type-dimension.md)
- [ ] [01 — Aggregation strategies (sum, last, max, unique-count, time_weighted_last)](01-aggregation-strategies.md)
- [ ] [02 — Plan + PlanVersion (immutable versioning)](02-plan-planversion.md)
- [ ] [03 — Pricing Components: Flat Fee, Per-Seat Fee, flat-rate Metered Charge](03-pricing-components-phase1.md)
- [ ] [04 — Catalog API surface (CRUD on Plans + PlanVersions, read on Dimensions)](04-catalog-api.md)
- [ ] [05 — Property tests for time_weighted_last aggregation](05-pbt-time-weighted-last.md)
