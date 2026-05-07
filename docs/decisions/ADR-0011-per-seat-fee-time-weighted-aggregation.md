---
id: ADR-0011
type: adr
state: accepted
title: Per-Seat Fee sources seat count from a `time_weighted_last` Dimension
date: 2026-05-06
deciders: [kim]
---

# ADR-0011 — Per-Seat Fee uses a `time_weighted_last` Dimension

## Context

The Per-Seat Fee Pricing Component (PRD §4.1, Phase 1) needs an answer for two questions:

1. **Where does the seat count come from?** The Subscription aggregate has no seat-count field, and a "current seat count on the Subscription" mutation method gives no Event history to derive seat-time from.
2. **How are mid-period seat changes billed?** End-of-period value over-bills downsizes; start-of-period value under-bills upsizes. For "10 seats Jan 1–14, 20 seats Jan 15–31, $10/seat monthly," the right answer is `(10 × 14 + 20 × 17) / 31 × $10 ≈ $154.84`, not $200 or $100.

Stripe and Orb both ship time-weighted seat aggregations.

## Decision

Per-Seat Fee sources its seat count from a Dimension on the Plan's Product Type whose Aggregation is the new `time_weighted_last`.

### New Aggregation: `time_weighted_last`

Adds a fifth Aggregation value alongside `sum`, `last`, `max`, `unique-count`.

- Events on a `time_weighted_last` Dimension define stepwise-constant intervals: between successive timestamps `t_i` and `t_{i+1}`, the value is the value reported at `t_i`.
- The Rollup carries `AggregatedValue = (∫ value(t) dt over [PeriodStart, PeriodEnd]) / (PeriodEnd − PeriodStart)` — the time-weighted average over the Billing Period.
- The integral is recomputed when Late Events arrive within the Late Event Window ([ADR-0010](ADR-0010-period-boundaries-and-final-close.md)).
- Bootstrap at `PeriodStart`: most recent Event with `timestamp <= PeriodStart` (looked up across the prior period if needed); otherwise `0`.
- Tail at `PeriodEnd`: the last Event's value extends to `PeriodEnd`.

### Per-Seat Fee Pricing Component shape

- Carries `unit_price` (a `Money` Value Object) and `dimension_code` (string).
- The PlanVersion factory validates at creation:
  - `dimension_code` references an existing Dimension on the Plan's Product Type.
  - That Dimension's Aggregation is `time_weighted_last`.
- At Final Close, the Line Item is `unit_price × Rollup.AggregatedValue` for the (Subscription, dimension_code, BillingPeriod) Rollup.

### Configurable Dimension code

The Dimension code is per-Pricing-Component, not hard-coded `seats`. Integrators name it `seats`, `users`, `licenses`, `editors`, etc. Multiple Per-Seat Fees on the same PlanVersion are allowed, each referencing a different Dimension.

### Per-Seat Fee remains a separate Pricing Component variant

Mathematically identical to a flat-rate Metered Charge over a `time_weighted_last` Dimension, but kept distinct (`Kind = PerSeat`) for Plan UI clarity, Invoice Line Item description ("Per-Seat Fee" reads better than "Metered Charge: seats"), and to allow seat-specific validation rules (e.g., "Per-Seat Fee never has a Quota").

### Phase plan

Lands in **Phase 1 — Metering MVP** alongside Per-Seat Fee, Flat Fee, and flat-rate Metered Charges. Adding it later would force an Aggregation migration on existing seat Dimensions.

## Consequences

- **Mid-period seat changes bill correctly.** Time-weighted average matches what every mature usage-billing platform produces.
- **No new aggregate field on Subscription.** Seat count flows through the same Event pipeline as everything else.
- **Late Event handling is uniform.** A late seat change recomputes the integral the same way a late metered usage Event recomputes a `sum`.
- **Cost: a new Aggregation type** with an additional integral accumulator on the Rollup. PBT (per [ADR-0005](ADR-0005-property-based-testing.md)) is mandatory: Late Events arriving in any order must produce the same integral.
- **Bootstrap value semantics need care.** A Subscription with no seat-count Event in the period has 0 seats — wrong. v1 documents the contract that the Integrator publishes a seat-count Event at Subscription creation; if it proves error-prone, the SubscriptionLifecycleService can raise a synthetic seat-count Event later.
- **Slight Plan UI complexity.** The Admin Dashboard guides Integrators to declare a `time_weighted_last` Dimension on the Product Type before adding a Per-Seat Fee.

### Out of scope

Per-day or per-hour seat granularity (works at whatever granularity Events are pushed); cross-Pricing-Component arithmetic ("first 10 seats free" = a tiered Metered Charge over a seat Dimension, Phase 3).
