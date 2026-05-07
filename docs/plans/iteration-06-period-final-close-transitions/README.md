# Iteration 06 — Period Boundaries, Final Close & Plan Transitions

## Goal

Replace the placeholder monthly-UTC Period anchor with the proper `CycleAnchor` model, implement `LateEventWindow` and `FinalClose`, and add `PlanTransition` with `Proration`. After this iteration, the platform has correct billing time semantics — but invoice generation itself ships in Iteration 07.

## Out of scope

No invoice generation (Iteration 07). No actual webhook delivery (Iteration 07). No tiered pricing (Iteration 08).

## Exit criteria

- `CycleAnchor` per Subscription: `anchor-day` or `calendar-month`, clamped to last-day-of-month per [ADR-0010](../../decisions/ADR-0010-period-boundaries-and-final-close.md).
- All Period math performed in the Integrator's Timezone.
- `LateEventWindow`: default 24h, override up to 7d, set per Plan or per Subscription. Configured at `PlanVersion` level by default; per-Subscription override allowed.
- `FinalClose = PeriodClose + LateEventWindow`. Rollups become immutable at FinalClose.
- `PlanTransition`: change of `PlanVersion` on a Subscription, immediate or next-period, with optional daily Proration.
- Property tests for boundary clamping, FinalClose monotonicity, Proration math.

## Issues

- [ ] [00 — CycleAnchor on Subscription (anchor-day, calendar-month, last-day clamping)](00-cycle-anchor.md)
- [ ] [01 — LateEventWindow at PlanVersion + per-Subscription override](01-late-event-window.md)
- [ ] [02 — FinalClose semantics on Rollups](02-final-close-rollups.md)
- [ ] [03 — PlanTransition with daily Proration](03-plan-transition-proration.md)
- [ ] [04 — Period scheduler / FinalClose worker](04-period-scheduler.md)
- [ ] [05 — Property tests (anchor clamping, FinalClose monotonicity, Proration)](05-pbt-period-final-close-proration.md)
