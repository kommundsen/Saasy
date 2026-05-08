---
title: Rollup aggregate + Rollup Updater worker
iteration: 03
status: todo
labels: [metering, domain, worker]
depends-on: [03-projections-subscriptions]
agent: backend
---

# Rollup aggregate + Rollup Updater worker

`Rollup` is an Aggregate Root in the Metering context, keyed by `(IntegratorId, SubscriptionId, DimensionId, Period)`. A worker consumes the raw event stream (or `metering.events` re-publication) and updates Rollups using the appropriate Aggregation strategy.

## Acceptance criteria

- Strongly-Typed `RollupId`.
- Period determination: for this iteration, use a fixed monthly anchor (calendar month, UTC) — proper Cycle Anchor logic is Iteration 06.
- `RollupUpdater` BackgroundService:
  - Reads new raw events.
  - Joins to Subscription projection to resolve PlanVersion → PricingComponents → which Dimensions matter.
  - Applies the Aggregation strategy from Iteration 02.
  - Writes/updates the Rollup row.
- Idempotent: replaying the same event yields the same Rollup state.
- Telemetry: rollup recompute count, aggregation latency.
