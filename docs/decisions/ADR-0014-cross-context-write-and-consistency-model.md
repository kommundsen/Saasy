---
id: ADR-0014
type: adr
state: accepted
title: Cross-context write & consistency model
date: 2026-05-08
deciders: [kim]
---

# ADR-0014 — Cross-context write & consistency model

## Context

[ADR-0016](ADR-0016-event-driven-projections.md) establishes that every consuming Bounded Context maintains its own projections of upstream contexts' state, populated via state-change events on the Internal Domain Event Bus ([ADR-0008](ADR-0008-internal-domain-event-bus.md)). It defines the read-side eventual-consistency contract at a high level (HTTP `425 Too Early`, `Retry-After: 1`) but leaves several adjacent decisions implicit:

- **Delete model.** Soft vs. hard delete; how projections reflect deletes; whether Child Entities follow the same rule as Aggregate Roots.
- **Read-side staleness contract.** Which endpoints return 425, how callers express freshness requirements, how the server distinguishes "missing forever" from "missing for now."
- **Write-time collisions.** What happens when a binding write (e.g. `POST /v1/subscriptions` referencing a `customer_id`) races a delete in the owning context.
- **Projection-lag SLO.** A concrete number that the higher-layer rules ("bounded by projection lag") can lean on, plus the alerting that backs it.
- **Per-aggregate ordering.** Whether per-Integrator FIFO from [ADR-0008](ADR-0008-internal-domain-event-bus.md) is sufficient, or whether stronger ordering is required.
- **Reconciliation / repair.** What recovery looks like when projections diverge from source-of-truth — consumer bug, lost event, owner-side drift.

This ADR settles each of those.

## Decision

### Delete model — uniform soft delete

All entities whose id is held by another context carry `DeletedAt: Instant?`. The field is set once and never unset.

- The only path to delete is a domain method `<Aggregate>.Delete(at)`, emitting `<aggregate>.deleted` on the per-context outbox.
- Repositories filter `DeletedAt IS NULL` by default; an explicit `IncludeDeleted()` opt-in supports historical reads (Invoice rendering, audit views).
- `ProjectionUpdater`s mirror the field: on `*.deleted`, the projection row's `deleted_at` is set, not removed.
- Writes against a soft-deleted referent are rejected by domain invariant in the owning context — not merely by a projection check on the consuming side.
- No undelete. Restoration is a new entity with a new id.

**Aggregate-internal Child Entities** — entities whose ids never escape the parent aggregate — follow whatever rule the parent decides, typically hard delete with the parent aggregate emitting a Domain Event describing the change. Examples: `ApiKey`, `ThresholdConfig`, `LineItem`, `CreditEntry`, ladder `Tier`s.

`PricingComponent` is treated as aggregate-internal: `PlanVersion` is immutable, so individual components are never deleted — a new `PlanVersion` is published instead. Soft-delete machinery on `PricingComponent` would be dead code.

### Read-side staleness — read-your-writes via ETag / `If-Match`

Extends and concretizes the 425 contract from [ADR-0016](ADR-0016-event-driven-projections.md).

- Every write response from an owning context carries `ETag: "<version>"` where `<version>` is the aggregate's stable per-row version (the `Version` field already stamped on outbox rows per [ADR-0016](ADR-0016-event-driven-projections.md) §projection idempotency).
- Each consuming context maintains a per-source **projection high-water mark** = the highest source-version it has projected.
- Callers may include `If-Match: "<version>"` on a follow-up request that depends on a projection. Server logic:
  - Projection high-water at-or-past requested version → serve normally (or `404` if the referent genuinely does not exist).
  - Projection high-water behind requested version → `425 Too Early`, `Retry-After: 1`, body shape per [ADR-0016](ADR-0016-event-driven-projections.md).
- When `If-Match` is omitted, the server falls back to a best-effort wall-clock check: if the projection's high-water is at-or-past "now" and the referent is missing, respond `404`; otherwise `425`.
- Client guidance: cap retries around 10 seconds; persistent 425 past that is operationally indistinguishable from `404`.
- 425 is reused from RFC 8470 with a Saasy-specific semantic. The original TLS 0-RTT meaning does not apply here; the body `code = dependency.not_yet_propagated` from [ADR-0016](ADR-0016-event-driven-projections.md) remains the stable contract for SDKs.
- 425 applies only to **cross-context-binding mutations** (e.g. `POST /v1/subscriptions` resolving a Customer). Pure read endpoints serve whatever the projection currently shows; staleness is an eventual-consistency property of the response, not an error.

### Write-time collisions

Most collision shapes fall out of the delete model + the read-your-writes contract. Only the in-flight-delete race needs an explicit policy.

- **Referent never existed.** Projection caught up to "now" + missing → `404`.
- **Referent created, projection lagging.** Handled by the `If-Match` / 425 path above.
- **Referent already deleted (projected).** Domain invariant rejects. Respond `409 Conflict` (entity exists, unusable) — distinct from `404`.
- **Referent being deleted, delete event in flight.** **Race-to-projection wins, eventual reconciliation.** The binding write proceeds against the still-live projection. When the consuming context later projects the `*.deleted` event, the `ProjectionUpdater` walks rows that referenced the now-deleted id, automatically cancels them with `reason = referent-deleted`, and emits a context-specific orphan event (e.g. `subscription.cancelled-on-orphan`) for ops visibility. The window is bounded by the lag SLO below; recovery is automatic.

A **synchronous owner-side validation hop** is the conservative fallback if the orphan window causes operational problems in production. It is not adopted by default — it would re-introduce the cross-context HTTP coupling that [ADR-0016](ADR-0016-event-driven-projections.md) explicitly avoids — but it is the documented mitigation invoked by the lag-ceiling alert below.

### Projection-lag SLO

Targets are end-to-end: `consumer_projection_commit_timestamp − producing_aggregate_commit_timestamp`. The producing aggregate stamps `committed_at` on its outbox row; the value is propagated through the bus envelope and recorded when the consumer commits the projection update.

- **Targets:** p95 < 2s, p99 < 10s.
- **Alert:** page when p99 > 30s sustained for 5 minutes.
- **Hard ceiling:** lag > 5 minutes OR no advance for 60 seconds for a context's relay/consumer → page + halt-affected-flows. Halting means switching the affected resource types to the synchronous owner-side validation fallback above until lag recovers.

**Mandatory instrumentation per consuming context:**

- Outbox dispatch lag histogram (relay segment).
- Projection consume lag histogram (consumer segment).
- Per-projection high-water gauge — the value the 425-vs-404 decision reads.
- Per-projection `last_advance_timestamp` gauge — drives the stuck-detection alert.
- All metrics tagged by `(producing_context, consuming_context, projection_name)`.

### Per-aggregate ordering

Per-Integrator FIFO is the ordering contract. No per-aggregate version vectors, no aggregate-id sessioning beyond the existing `integrator_id` session keying from [ADR-0008](ADR-0008-internal-domain-event-bus.md).

Within an Integrator, all events from one producing context arrive at any consumer in commit order. Events across contexts can interleave, but each context-stream is serial.

**Configuration requirements (enforced in code review):**

- Service Bus subscriptions are session-aware with `MaxConcurrentCallsPerSession = 1`.
- Outbox relays dispatch in `outbox.id` order. Sequence is assigned at commit, so dispatch order equals commit order.
- Single active dispatcher per `(context, integrator_id)` partition. Multi-instance relays must use leader-election or work-stealing that respects this constraint.

Per-Integrator FIFO is sufficient because every aggregate is owned by exactly one context — no aggregate is mutated from two emitters — so per-context FIFO within an Integrator implies per-aggregate FIFO. Cross-context dependencies (e.g. Subscription depends on Plan projection) are handled by the read-your-writes contract above, not by ordering on the bus.

### Reconciliation / repair

Three failure modes the design must survive:

- **Consumer bug.** Projection rows are wrong; source + event log are correct. Recovery: replay events into the fixed consumer.
- **Lost events.** Gap in the projection — missed message, advanced checkpoint past unprocessed work. Recovery: targeted re-dispatch from the outbox.
- **Owner-side drift.** Source of truth itself rolled back or corrupted (e.g. database restore from backup). No automated recovery.

**v1 toolkit:**

- **The outbox is the durable log.** Service Bus is transport, not the system of record. Events live in per-context outbox tables for at least 90 days; older entries archive to blob storage (cold) and prune. The 90-day retention + cold-archive policy is recorded in [ADR-0017](ADR-0017-outbox-retention-and-cold-archive.md) so it can evolve independently.
- **Replay tooling ships in v1.** Per-context operator-only endpoint accepts `(consumer_name, from_outbox_id, to_outbox_id)` and re-dispatches the range without modifying outbox rows. Audit-logged.
- **Targeted rebuild.** Per-Integrator, per-projection truncate followed by replay from `outbox.id = 0` for the affected sources.
- **Idempotency is a test invariant**, not just a convention. Every consumer carries a property test asserting that applying the same event twice against an already-applied state is a no-op.
- **Snapshot mechanism remains v2.** New post-launch contexts bootstrap from the 90-day outbox window. Older state is reconstructed only when needed; most queries are forward-looking.
- **Owner-side drift has no self-healing path.** Documented in `docs/runbooks/source-rollback.md`. Database restores are a high-context human decision; automating them would be unsafe.

## Consequences

### Wins

- **The eventual-consistency contract is now mechanical, not vibes.** Callers that need read-your-writes use `If-Match`; the server gives a precise causal answer.
- **Soft delete is uniform** for every cross-context entity, removing a class of "did we forget to filter `deleted_at`?" bugs by making the filter the default.
- **Orphan recovery is automatic** in the common in-flight-delete race; operators are involved only when the lag ceiling is breached.
- **The lag SLO is concrete enough to alert on**, and the high-water gauge it produces is the same value the 425-vs-404 decision reads — one signal, two purposes.
- **Per-Integrator FIFO is enough**, so we ship without per-aggregate ordering machinery.
- **Replay is a v1 capability**, so consumer bugs and lost events are fixable in normal operations rather than incidents.

### Costs

- **The 90-day outbox window** is real disk pressure on Postgres. Bounded by archive + prune.
- **Replay tooling needs careful operator UX.** Misuse can flood consumers; the endpoint is operator-only and audit-logged, but a bad replay is still self-inflicted.
- **`If-Match` is opt-in for clients.** SDKs that don't thread ETags through will fall back to the wall-clock heuristic — adequate but coarser.
- **Halt-and-fall-back at the lag ceiling** introduces a code path that's exercised rarely. The synchronous validation hop must be tested under load, not only in production incidents.
- **Property tests for consumer idempotency** are mandatory boilerplate per consumer.
- **Owner-side drift requires human judgment.** The choice not to automate this is deliberate but means the `source-rollback` runbook must stay current.

### Out of scope

- Snapshot / replay for bootstrapping new contexts post-launch (deferred to v2).
- Real-time event subscriptions for live UI displays (deferred to v2 per [ADR-0016](ADR-0016-event-driven-projections.md)).
- Cross-Integrator ordering. Sessions are keyed on `integrator_id`; events across Integrators have no defined order.
- Outbox retention and cold-archive specifics — covered by [ADR-0017](ADR-0017-outbox-retention-and-cold-archive.md).
