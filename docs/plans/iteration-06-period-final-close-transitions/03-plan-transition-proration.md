---
title: PlanTransition with daily Proration
iteration: 06
status: todo
labels: [subscriptions, domain]
depends-on: [00-cycle-anchor]
agent: backend
---

# PlanTransition with daily Proration

Change of `PlanVersion` on a Subscription. Two timing options: `Immediate` or `NextPeriod`. Two proration options: `None` or `Daily` ([ADR-0010](../../decisions/ADR-0010-period-boundaries-and-final-close.md)).

## Acceptance criteria

- `Subscription.TransitionPlan(toPlanVersionId, timing, proration, effectiveAt)`.
- `Immediate` with `Daily`: closes the current Period at `effectiveAt`, opens a new Period under the new PlanVersion, `RolledOver` Rollups are split per Pricing Component (FlatFee + PerSeatFee both prorated by elapsed/total days; MeteredCharge usage allocated to its Period of occurrence).
- `Immediate` with `None`: no proration; Pricing Components for the Period switch wholesale at `effectiveAt`.
- `NextPeriod`: scheduled — applied at the next Period start.
- Plan currency must match between old and new PlanVersion (same Plan, different Version is a republish, not a Transition).
- Cross-ProductType transitions forbidden ([ADR-0015](../../decisions/ADR-0015-customer-subscription-cardinality.md)).
- Domain Event `subscription.plan-transitioned` emitted.
