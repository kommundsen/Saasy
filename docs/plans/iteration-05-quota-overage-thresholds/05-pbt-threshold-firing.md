---
title: Property tests for Threshold firing
iteration: 05
status: todo
labels: [metering, testing, pbt]
depends-on: [02-threshold-firing-logic, 04-threshold-catchup-on-add]
agent: backend
---

# Property tests for Threshold firing

## Acceptance criteria

- Generators: arbitrary event streams, arbitrary ThresholdConfigs (with adds/removes mid-Period), arbitrary Quota.
- Properties:
  - **At-most-once per Period** — for any `(SubscriptionId, DimensionId, Period, Percent)`, at most one `usage.threshold.crossed` ever emitted.
  - **Monotone in usage** — increasing usage can only add fires, never remove.
  - **Period reset** — fires from Period N do not appear in Period N+1's state.
  - **Catch-up correctness** — adding a level fires it iff current usage ≥ that level AND it was not fired earlier in the same Period.
  - **Order independence (within Period)** — applying events + threshold-add ops in any consistent order yields the same set of fires.
- Failures shrink to minimal counterexample.
