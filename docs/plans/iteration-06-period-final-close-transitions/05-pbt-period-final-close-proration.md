---
title: Property tests (anchor clamping, FinalClose monotonicity, Proration)
iteration: 06
status: todo
labels: [testing, pbt]
depends-on: [00-cycle-anchor, 02-final-close-rollups, 03-plan-transition-proration]
---

# Property tests

## Acceptance criteria

- **CycleAnchor clamping**: for any anchor-day ∈ [1,31] and any month, `GetCurrentPeriod` returns a `(start, end)` whose end-day equals min(anchor-day-1, last-day-of-month).
- **FinalClose monotonicity**: for any Subscription and increasing wall-clock instants, `Status` transitions only Open → Closed → FinalClosed; never reverts.
- **Proration sums to whole**: for any Daily proration split at any point in a Period, the prorated FlatFee/PerSeatFee charges across split sub-periods sum (within rounding tolerance documented in ADR) to the un-split charge.
- **No double-charge across Transitions**: an Immediate Transition does not produce overlapping Pricing Components for the same instant.
- **Timezone roundtrip**: Period boundaries computed in Integrator timezone roundtrip through UTC without drift.
