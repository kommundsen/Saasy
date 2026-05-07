---
title: Property tests for Rollup correctness
iteration: 03
status: todo
labels: [metering, testing, pbt]
depends-on: [04-rollup-aggregate-worker]
---

# Property tests for Rollup correctness

Rollup correctness underpins everything downstream (Threshold firing, Invoice line items). Cover ordering, duplicates, late arrivals (within Iteration 03's no-Final-Close model).

## Acceptance criteria

- Generators: arbitrary event streams per Subscription/Dimension across multiple Periods.
- Properties:
  - **Order independence** — applying events in any order yields the same Rollup state per Aggregation that is order-independent (Sum, Max, UniqueCount). For `Last` and `TimeWeightedLast`, applying by `occurred_at` order yields the same result regardless of arrival order.
  - **Idempotence** — replaying the entire stream produces the same Rollup.
  - **Period isolation** — events from Period P1 do not affect Rollup for P2 except via `TimeWeightedLast`'s carry-forward.
  - **Subscription isolation** — events for Subscription A do not influence Rollup for Subscription B.
- Failures shrink to minimal counterexample; CI seed pinned per ADR-0005.
