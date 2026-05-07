---
title: CycleAnchor on Subscription
iteration: 06
status: todo
labels: [subscriptions, domain]
depends-on: []
---

# CycleAnchor on Subscription

`CycleAnchor` is `AnchorDay(int day) | CalendarMonth`. For `AnchorDay`, when the target day exceeds the month's last day, clamp to last-day-of-month per [ADR-0010](../../decisions/ADR-0010-period-boundaries-and-final-close.md). All Period math uses the Integrator's `Timezone`.

## Acceptance criteria

- `CycleAnchor` value object with two cases.
- Pure function `BillingPeriod GetCurrentPeriod(CycleAnchor, Timezone, Instant now)` returning `(start, end)` instants.
- Clamping rule documented and tested: anchor-day=31 in February returns Feb 28/29.
- `Subscription.Create` requires a `CycleAnchor`.
- Backwards-compat path: existing Iteration-03 monthly-UTC Subscriptions migrate to `CalendarMonth` with default integrator timezone.
