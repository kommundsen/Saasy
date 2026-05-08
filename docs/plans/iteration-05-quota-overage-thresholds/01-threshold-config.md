---
title: ThresholdConfig on Subscription
iteration: 05
status: todo
labels: [subscriptions, domain]
depends-on: []
agent: backend
---

# ThresholdConfig on Subscription

`ThresholdConfig` is a Child Entity of `Subscription`: per Dimension, a list of percent levels (`0 < p ≤ 1`) at which to fire when `AggregatedValue / Quota` crosses.

## Acceptance criteria

- `Subscription.AddThreshold(dimensionId, percent)` validates dimension is part of the PlanVersion's MeteredCharge components and percent ∈ (0, 1].
- `RemoveThreshold(thresholdId)` supported; in-flight ThresholdState is preserved (removing config does not retroactively un-fire).
- Duplicate `(dimensionId, percent)` rejected.
- API: `POST /v1/subscriptions/{id}/thresholds`, `DELETE /v1/subscriptions/{id}/thresholds/{thresholdId}`.
