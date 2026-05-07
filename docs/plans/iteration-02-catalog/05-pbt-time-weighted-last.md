---
title: Property tests for TimeWeightedLast aggregation
iteration: 02
status: todo
labels: [catalog, testing, pbt]
depends-on: [01-aggregation-strategies]
---

# Property tests for `TimeWeightedLast`

`TimeWeightedLast` underpins Per-Seat Fee billing — correctness here directly affects invoice money. Cover with property-based tests before any Subscription/Rollup code consumes it.

## Acceptance criteria

- Generators produce arbitrary event streams within a Period (any number of events, including zero, including a single event spanning the full period).
- Properties:
  - **Constant signal** — if all events have value `v`, aggregate = `v`.
  - **Boundary preservation** — adding/removing events outside the Period does not change the result.
  - **Linearity** — for two non-overlapping sub-periods, `agg(P1 ∪ P2)` equals the time-weighted combination.
  - **No events ⇒ "carry-forward"** — uses the most recent event before Period start; if none, returns 0 with a documented sentinel for downstream pricing.
  - **Idempotence** — duplicate events at the same instant collapse to one.
- Failures shrink to minimal counterexample.

## References

- [ADR-0011](../../decisions/ADR-0011-per-seat-fee-time-weighted-aggregation.md)
- [ADR-0005](../../decisions/ADR-0005-property-based-testing.md)
