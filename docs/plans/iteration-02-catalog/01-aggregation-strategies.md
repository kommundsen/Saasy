---
title: Aggregation strategies (sum, last, max, unique-count, time_weighted_last)
iteration: 02
status: todo
labels: [catalog, metering, domain]
depends-on: [00-product-type-dimension]
---

# Aggregation strategies

Implement the five Aggregation strategies as pure functions over an event stream window. These are reused by Rollup workers in Iteration 03; landing them in Catalog with thorough property tests now keeps the math isolated.

## Acceptance criteria

- `IAggregationStrategy` with method `Aggregate(IEnumerable<DimensionEvent>, Period) -> AggregatedValue`.
- Implementations:
  - `Sum` — sum of `value`.
  - `Last` — value of the latest event by `occurred_at`.
  - `Max` — max `value`.
  - `UniqueCount` — count of distinct `value`s (string-coerced).
  - `TimeWeightedLast` — time-weighted average where each `value` is held until the next event or period end ([ADR-0011](../../decisions/ADR-0011-per-seat-fee-time-weighted-aggregation.md)).
- Pure functions: no DB / time-of-day dependencies.
- Property tests for each (see issue 05 for the headline `TimeWeightedLast` suite).
