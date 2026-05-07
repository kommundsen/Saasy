---
title: FinalClose semantics on Rollups
iteration: 06
status: todo
labels: [metering, domain]
depends-on: [00-cycle-anchor, 01-late-event-window]
---

# FinalClose semantics

After `PeriodClose + LateEventWindow`, a Rollup is sealed: no further updates. Late Events arriving past FinalClose are recorded against the Rollup's `LateAfterCloseLog` for audit, but do not change `AggregatedValue`.

## Acceptance criteria

- Rollup gains `Status: Open | Closed | FinalClosed` and `FinalClosedAt: Instant?`.
- `RollupUpdater` rejects mutations when `FinalClosed`; logs the late event.
- Domain Event `period.final-closed` published per `(SubscriptionId, Period)` at FinalClose.
- Tests cover the boundary at exactly `FinalClosedAt` and 1ms after.
- Audit log entry for any late-after-close event.
