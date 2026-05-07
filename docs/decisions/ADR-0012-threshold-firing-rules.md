---
id: ADR-0012
type: adr
state: accepted
title: Threshold firing — state-based, once-per-period, catch-up on add
date: 2026-05-07
deciders: [kim]
---

# ADR-0012 — Threshold firing rules

## Context

Saasy alerts Integrators that a Customer crossed a usage Threshold via the `usage.threshold.crossed` Webhook. Each Subscription has a `ThresholdConfig` per metered Dimension carrying percentages (defaults `0.5, 0.8, 1.0, 1.1`); the Rollup carries `ThresholdState` recording which percentages have fired this period.

"Each Threshold fires exactly once per period" leaves three operational questions unresolved:

1. **Add a percentage mid-period that's already crossed.** Rollup at 80%, Integrator adds `0.7`. Does it fire?
2. **Remove a percentage that fired, then re-add within the same period.** Does it re-fire?
3. **Late Events that produce a crossing after Period Close (still inside the Window).** Do they fire?

Pure edge-detection produces an onboarding wart: an Integrator configures their first Threshold at 80%, sees the Rollup at 90%, and gets no confirming Webhook because Saasy never observed a transition. Pure state-detection risks oscillation. The right shape is a hybrid: state-check on every Rollup recompute, with a once-per-(period, percentage) record preventing re-fires.

## Decision

State-based firing with a once-per-period record, plus a catch-up fire on Threshold add propagated cross-context via Domain Event.

### Firing rule

After every Rollup recompute (live ingestion, Late Event arrival, or `ThresholdAdded` consumed by Metering), for each percentage `p` in the active `ThresholdConfig`:

```
if (AggregatedValue / Quota >= p) AND (p ∉ ThresholdState):
    fire `usage.threshold.crossed` (payload includes p, Rollup value, Quota, period)
    ThresholdState ← ThresholdState ∪ {p}
```

State-based; once-per-period per percentage; subsumes edge detection for normal growth; handles Late Events automatically; handles mid-period add when invoked at config time.

### Add path (cross-context Domain Event)

`Subscription.AddThreshold(DimensionCode, percentage, TimeProvider)` raises a `ThresholdAdded` Domain Event carrying `(SubscriptionId, IntegratorId, DimensionCode, percentage, occurred_at)`. It flows through the standard pipeline:

1. Subscriptions' `outbox_messages` receives the row in the same transaction as the `ThresholdConfig` mutation.
2. The Subscriptions Outbox Relay publishes to `saasy.domain-events.subscriptions`.
3. **Metering subscribes to that topic** (alongside the Webhook Dispatcher). On receipt, Metering loads the Rollup and runs the firing rule for the new percentage.

This is the first cross-context Domain Event consumption in Saasy: Metering becomes a consumer of Subscriptions events, not just a producer of its own. The bus topology already supports this.

### Remove path

`Subscription.RemoveThreshold(...)` removes the percentage from `ThresholdConfig`. The corresponding entry in `Rollup.ThresholdState` is **NOT** cleared. Re-adding the same percentage within the same period does not re-fire it. Removing then re-adding is not a fresh subscription to "tell me again."

`ThresholdRemoved` is raised; Metering does not need to act on it.

### Period boundary

`ThresholdState` is keyed by `(SubscriptionId, DimensionCode, BillingPeriodStart)`. New period starts with empty state. Late Events for the prior period apply to the prior period's Rollup and state. After Final Close, the prior period's Rollup is frozen; no further firing regardless of config changes.

### Edge cases

- **Add a percentage already past the Rollup, where the Rollup later drops below it (refund).** Add fires immediately. Drop does not un-fire. Re-rise does not re-fire.
- **Remove a percentage about to fire (Rollup at 79%, `0.8` configured, Integrator removes `0.8`).** No fire.
- **Quota changes mid-period.** A Quota change implies a Plan Transition; with `effect: immediate`, the period rolls over and a new Rollup starts. No mid-period Quota change to handle.
- **Concurrent Rollup + ThresholdConfig update.** Optimistic concurrency token serializes; rule's correctness depends only on final `(AggregatedValue, ThresholdState, ThresholdConfig)`.

## Consequences

- **Onboarding works as expected.** Configuring a Threshold and seeing a confirming Webhook makes the integration feel correct from the first try.
- **Mental model is uniform.** "Fires when usage is at or past, once per period, no matter how it got there."
- **Oscillation is bounded** by the once-per-period record.
- **Cross-context Domain Event consumption pattern is exercised.** Validates the bus design without architectural rework.
- **Late Event interaction is automatic** — no special-case code.
- **Cost: cross-context coupling.** One new bus subscription in Metering, plus a `ThresholdAddedHandler`. Mitigated by the bus topology already designed for this.
- **Cost: catch-up fire on add loads the Rollup synchronously** — one DB read per add. Acceptable.
- **`ThresholdState` survives across config edits within a period, by design.** The Admin Dashboard should explain this when an Integrator manipulates Thresholds mid-period.
- **Webhook payload includes `event_id`** so Integrators can dedupe; once-per-period record ensures Saasy never sends a duplicate for the same `(period, percentage)`.

### Out of scope

Digest mode for Threshold Webhooks; per-Threshold custom payloads; Threshold-driven enforcement (Saasy is Soft Enforcement only).
