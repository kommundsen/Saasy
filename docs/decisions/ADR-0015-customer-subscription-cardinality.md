---
id: ADR-0015
type: adr
state: accepted
title: Customer-Subscription cardinality — zero-or-more by default, opt-in per-Plan exclusivity
date: 2026-05-07
deciders: [kim]
---

# ADR-0015 — Customer-Subscription cardinality

## Context

Both "exactly one" and "at most one" Subscription per Customer forbid scenarios that are common in real B2B SaaS:

- Multiple Subscriptions on the **same Plan** (e.g., two independent licenses).
- Subscriptions across **multiple Product Types** (e.g., `compute` + `storage` concurrently).
- A **trial Subscription parallel to a paid Subscription**.
- A **migration overlap window** with two active Subscriptions during cutover.

Every mature platform (Stripe, Lago, Orb, Metronome) allows multiple concurrent Subscriptions per Customer. A "one Subscription per Customer per Product Type" variant still rejects multi-Subscription-on-same-Plan and parallel-Plan scenarios.

The right shape: **no structural cardinality by default; Plans that genuinely need exclusivity opt in.**

## Decision

### Default: zero or more active Subscriptions per Customer

The data model imposes no per-Customer cardinality limit. A Customer may hold any number of active Subscriptions concurrently, in any combination consistent with the other invariants (Currency match, etc.).

### Opt-in exclusivity at the Plan level

`Plan` carries a boolean `IsExclusivePerCustomer` (default `false`). When `true`:

> A Customer may hold at most one active Subscription whose PlanVersion's PlanId equals this Plan's PlanId.

Enforcement:

- **Domain check.** `SubscriptionLifecycleService.CreateAsync` loads the target Plan, checks `Plan.IsExclusivePerCustomer`, and if `true` calls `ISubscriptionRepository.HasActiveSubscriptionForPlanAsync(customerId, planId)`. On match, returns `Result.Conflict("Plan is exclusive; Customer already has an active Subscription on this Plan.")`.
- **Database constraint deferred to v2.** A trigger-based predicate-aware unique check is the right shape but operationally complex; v1 relies on the Repository check + the optimistic concurrency token. Race window is narrow at v1 design-partner volume; an asynchronous reconciliation check catches the worst case.

### Plan Transitions stay within one Product Type

A Subscription's Product Type is set at creation (from its initial PlanVersion's Plan) and never changes. `SubscriptionLifecycleService.TransitionAsync` validates that the target PlanVersion's Plan references the same ProductTypeId; cross-Product-Type changes are rejected.

Reasons: (1) Rollups are keyed by Dimensions belonging to the Product Type, and mid-flight Product Type change would orphan or require translating Dimension code spaces. (2) "What am I paying for?" is tied to Product Type. Customers wanting to change Product Type cancel and create a new Subscription.

### Lifetime exclusivity is deferred

"Only one trial Subscription per Customer EVER" requires Customer-level historical state surviving Subscription deletion. v1 ships concurrent exclusivity only.

### Multi-Subscription billing UX

Each active Subscription generates its own Invoice at its own Final Close. There is no consolidated "one Invoice per Customer per Period" — Subscriptions can have different Cycle Anchors / Intervals. The Customer Portal may visually group concurrent Invoices; data model is per-Subscription.

## Consequences

- **No arbitrary cardinality cap.** Default behavior matches realistic B2B SaaS shape.
- **Multi-product Integrators, trials, migrations work natively.**
- **Exclusivity is configurable per-Plan** for genuine "one-tier-per-Customer" requirements.
- **Plan Transition rules stay simple** — no cross-Product-Type complexity.
- **Cost: no DB-level enforcement of exclusivity in v1.** Repository check + optimistic concurrency is the v1 enforcement; trigger-based DB enforcement is a clear later step when scale justifies it.
- **`IsExclusivePerCustomer` is mutable on Plan but does not retroactively cancel existing Subscriptions.** Flipping `false` → `true` while two active Subscriptions on that Plan exist for a Customer continues to allow those two; constraint applies to new creates only. The Admin Dashboard surfaces a warning.
- **Customers with many active Subscriptions are operationally heavier.** Acceptable for v1.
- **Invariant relies on application-layer code.** Mitigated by the rule living in one Domain Service plus PBT (per [ADR-0005](ADR-0005-property-based-testing.md)) generating Subscribe → Cancel → Subscribe sequences against exclusive Plans.

### Out of scope

Lifetime exclusivity; per-Plan-Version exclusivity; per-Product-Type cardinality caps; per-Integrator-default exclusivity; bulk-create for migrations; billing consolidation across Subscriptions (UI concern).
