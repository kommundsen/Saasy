---
title: Threshold firing logic + state on Rollup
iteration: 05
status: todo
labels: [metering, domain]
depends-on: [01-threshold-config]
agent: backend
---

# Threshold firing logic + state on Rollup

State-based firing per [ADR-0012](../../decisions/ADR-0012-threshold-firing-rules.md): a level `p` fires when `AggregatedValue / Quota >= p` AND `p` is not already in the Rollup's `ThresholdState` set for the current Period. Once fired, recorded in state until the Period rolls and state resets.

## Acceptance criteria

- `Rollup.ThresholdState: Set<percent>` persisted per Rollup row.
- `RollupUpdater` evaluates configured Thresholds after each aggregation update; emits `usage.threshold.crossed` per newly-fired level.
- New Period (Iteration 06 anchors; for now monthly UTC) ⇒ `ThresholdState` resets to empty.
- No backwards firing: if usage decreases below `p` after firing, `p` does not re-fire in the same Period.
- All operations transactional within Metering context; published via Outbox.
