# Iteration 05 — Quota, Overage & Thresholds

## Goal

Extend the Metered Charge Pricing Component with `Quota` and `Overage` (still flat-rate; tiers come in Iteration 08). Add `ThresholdConfig` to Subscriptions and implement state-based, once-per-period Threshold firing per [ADR-0012](../../decisions/ADR-0012-threshold-firing-rules.md). Emit the `usage.threshold.crossed` Domain Event.

## Out of scope

No actual Webhook delivery to the Integrator — the Domain Event lands on the bus; outbound Webhook pipeline is Iteration 07. No Plan Transitions yet.

## Exit criteria

- `MeteredCharge` Pricing Component grows `QuotaPerPeriod` (decimal) and `OverageRate` (Money/unit). Backwards compatible with flat-rate (Quota = 0 ⇒ all usage is overage at the only rate; Quota > 0, OverageRate=null ⇒ hard cap behavior is left for ADR if needed — for now reject this configuration).
- `ThresholdConfig` (e.g., 50%, 80%, 100%) attached to a Subscription per Dimension.
- Threshold state persisted on the Rollup; firing is idempotent: `(SubscriptionId, DimensionId, Period, Percent)` fires at most once per Period.
- Cross-period reset: new Period, new ThresholdState ([ADR-0012](../../decisions/ADR-0012-threshold-firing-rules.md)).
- Catch-up on add: adding a Threshold mid-period below current usage immediately fires for all not-yet-fired levels at-or-below.
- Domain Event `usage.threshold.crossed` published via Outbox to `metering.events`.
- Property tests for firing rules.

## Issues

- [ ] [00 — Quota + Overage on MeteredCharge](00-quota-overage-metered.md)
- [ ] [01 — ThresholdConfig on Subscription](01-threshold-config.md)
- [ ] [02 — Threshold firing logic + state on Rollup](02-threshold-firing-logic.md)
- [ ] [03 — `usage.threshold.crossed` Domain Event](03-threshold-crossed-event.md)
- [ ] [04 — Catch-up on add behavior](04-threshold-catchup-on-add.md)
- [ ] [05 — Property tests for Threshold firing](05-pbt-threshold-firing.md)
