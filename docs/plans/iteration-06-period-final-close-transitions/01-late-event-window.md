---
title: LateEventWindow at PlanVersion + per-Subscription override
iteration: 06
status: todo
labels: [catalog, subscriptions, domain]
depends-on: []
agent: backend
---

# LateEventWindow

Default 24h, max 7d. Set on PlanVersion; can be overridden per Subscription.

## Acceptance criteria

- `LateEventWindow: TimeSpan` on PlanVersion (default 24h).
- `Subscription.LateEventWindowOverride: TimeSpan?` optional; capped at 7d.
- `Subscription.EffectiveLateEventWindow` resolves override → PlanVersion default.
- Validation rejects > 7d.
- API exposes both fields; Admin Dashboard read-only view shows effective value.
