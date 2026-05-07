---
title: Minimum Pricing Component + top-up LineItem
iteration: 11
status: todo
labels: [catalog, billing]
depends-on: []
---

# Minimum Pricing Component

`Minimum` is a Pricing Component on PlanVersion: `MinimumPerPeriod: Money`. If the sum of LineItems for the Period (excluding Minimum and Credits) is below the floor, append a "Minimum top-up" LineItem to bring the total to the floor.

## Acceptance criteria

- New PricingComponent kind `Minimum`.
- Currency must match Plan currency.
- Top-up appears as a single LineItem with `Description = "Period minimum top-up"`.
- If sum already exceeds the floor, no LineItem appears (idempotent in the no-op case).
- Per-Period: applies only to the closing Period at Invoice generation.
