---
title: Period scheduler / FinalClose worker
iteration: 06
status: todo
labels: [metering, worker]
depends-on: [02-final-close-rollups, 03-plan-transition-proration]
agent: backend
---

# Period scheduler / FinalClose worker

A BackgroundService that, per Subscription, advances Periods at `PeriodClose`, marks Rollups `Closed`, then `FinalClosed` after the LateEventWindow elapses, and processes scheduled `NextPeriod` transitions.

## Acceptance criteria

- Worker scans Subscriptions whose current Period crosses now (per Integrator timezone).
- Idempotent: re-running yields no duplicate Period rows or Domain Events.
- Honors paused/cancelled Subscriptions (no new Periods).
- Telemetry: `period_close_lag_seconds` gauge + `final_close_emitted_total` counter.
