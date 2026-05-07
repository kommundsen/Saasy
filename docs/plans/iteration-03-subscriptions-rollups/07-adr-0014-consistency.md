---
title: ADR-0014 — cross-context consistency model
iteration: 03
status: todo
labels: [architecture, adr]
depends-on: []
---

# ADR-0014 — cross-context consistency model

Write the ADR before the iteration ships. Capture the decision on what happens when a referenced aggregate is mutated/deleted in a source context after a consumer has projected it (e.g., Customer deleted after a Subscription is created against the projection).

## Acceptance criteria

- Decision recorded in `docs/decisions/ADR-0014-<slug>.md`.
- Covers:
  - Whether deletes are soft (preferred) or hard, and how projections reflect this.
  - HTTP 425 / retry semantics on the read side when projection is stale.
  - What "stale projection" means for Subscription create (block vs. allow with reconciliation).
  - Outbox delivery guarantees and how they interact with projection lag.
- Listed in [iterations.md](../iterations.md) under "Open decisions" gets crossed off.

## Decisions so far (converging towards the ADR draft)

### Delete model — uniform soft delete

- All entities whose id is held by another context (Aggregate Roots, plus any cross-context-referenced Child Entity) carry `DeletedAt: Instant?`, set once, never unset.
- Domain method `<Aggregate>.Delete(at)` is the only path; emits `<aggregate>.deleted` Domain Event.
- Repositories filter `DeletedAt IS NULL` by default; explicit `IncludeDeleted()` opt-in for historical reads (Invoice rendering, audit views).
- Projections mirror the field. `ProjectionUpdater` on `*.deleted` sets `deleted_at` rather than removing the row.
- Writes against a soft-deleted referent are rejected at the owning context — domain invariant, not just a projection check.
- No undelete. Restoration is a new entity with a new id.
- **Aggregate-internal Child Entities** (ids never escape the aggregate) follow whatever the parent aggregate decides — typically hard delete with the parent emitting a Domain Event describing the change. Examples: ApiKey, ThresholdConfig, LineItem, CreditEntry, Tier.
- **PricingComponent** is aggregate-internal (PlanVersion is immutable; you publish a new PlanVersion rather than mutating components), so no soft-delete machinery on it.

### Read-side staleness — read-your-writes consistency tokens via ETag

- Write responses include `ETag: "<version>"` where `<version>` is a stable per-aggregate version (e.g. outbox sequence number for that aggregate).
- Consuming contexts track per-source-context **projection high-water mark** = the highest source-version ingested.
- Clients may include `If-Match: "<version>"` on follow-up requests that depend on a projection. Server compares the requested version against the projection high-water:
  - At-or-past requested version → serve normally (or 404 if the referent genuinely does not exist).
  - Behind requested version → `425 Too Early` with `Retry-After: 1`.
- When `If-Match` is omitted, server falls back to a best-effort high-water-vs-wall-clock check (caught up to "now" → 404 if missing; behind → 425).
- Client guidance: cap retries around 10s; persistent 425 past that is operationally treated as 404.
- 425 is reused from RFC 8470 with a documented Saasy semantic — its original TLS-0-RTT meaning does not apply here.

### Write-time collision rules

Most collision shapes fall out of the delete model + the read-your-writes contract; only the in-flight-delete race needs an explicit policy.

- **Referent never existed** — projection caught up to "now" + missing → `404`.
- **Referent created, projection lagging** — handled by the `If-Match` / 425 path above.
- **Referent already deleted (projected)** — domain invariant rejects; respond `409 Conflict` (entity exists, unusable).
- **Referent being deleted, delete event in flight (projected as live, deleted at source)** — **race-to-projection wins, eventual reconciliation:**
  - The Subscription (or other binding) create proceeds against the live projection.
  - When the consuming context later projects the `*.deleted` event, it walks any rows that referenced the now-deleted id, automatically cancels them with `reason = referent-deleted`, and emits a context-specific orphan event (e.g. `subscription.cancelled-on-orphan`) for ops visibility.
  - Window is bounded by the projection-lag SLO (Q4); failure mode is recoverable without manual intervention.
- **Synchronous owner-side validation hop** is the conservative fallback if the orphan window causes operational problems in production. Not adopted by default to preserve the no-cross-context-HTTP rule from [ADR-0016](../../decisions/ADR-0016-event-driven-projections.md).

### Outbox + projection-lag SLO

Targets, alerting, and instrumentation that back the bounds asserted in Q1–Q3.

**SLO targets (end-to-end: owner commit → consumer projection commit):**
- p95 < 2s
- p99 < 10s

**Alerting:**
- Page when p99 > 30s sustained for 5 minutes.
- **Hard ceiling**: lag > 5 minutes OR no advance in 60s for a context's relay/consumer → page + halt-affected-flows. Halting means switching the affected resource types to the conservative fallback from Q3 (synchronous owner-side validation hop) until lag recovers.

**Instrumentation (mandatory per consuming context):**
- Per-context outbox dispatch lag histogram (segment 1 + 2 of pipeline).
- Per-projection consume lag histogram (segment 3).
- Per-projection high-water gauge — the value 425-vs-404 decisions read.
- Per-projection `last_advance_timestamp` gauge — drives the stuck-detection alert.
- All metrics tagged by `(producing_context, consuming_context, projection_name)`.

**Lag definition:**
End-to-end lag = `consumer_projection_commit_timestamp − producing_aggregate_commit_timestamp`. The producing aggregate stamps `committed_at` on its outbox row; the value is propagated through the bus envelope and recorded when the consumer commits the projection update.

### Per-aggregate ordering

**Per-Integrator FIFO is the ordering contract.** No per-aggregate version vectors, no aggregate-id sessioning. Within an Integrator, all events from one producing context arrive at any consumer in commit order; events across contexts can interleave but each context-stream is serial.

**Configuration requirements (enforced in code review):**
- Service Bus subscriptions are session-aware with `MaxConcurrentCallsPerSession = 1`. Sessions key on `integrator_id` per [ADR-0008](../../decisions/ADR-0008-internal-domain-event-bus.md).
- Outbox relays dispatch in `outbox.id` order (sequence assigned at commit, so dispatch order = commit order).
- Single active dispatcher per `(context, integrator_id)` partition. Multi-instance relays must use leader-election or work-stealing that respects this.

**Why per-Integrator FIFO suffices:** every aggregate is owned by exactly one context (no aggregate is mutated from two emitters), so per-context FIFO within an Integrator implies per-aggregate FIFO. Cross-context dependencies (e.g. Subscription depends on Plan projection) are handled by the read-your-writes contract in Q2, not by ordering guarantees on the bus.

### Reconciliation / repair

Three failure modes the design must survive:

- **Consumer bug** — projection rows wrong; source + event log correct. Recovery: replay events into the fixed consumer.
- **Lost events** — gap in the projection (missed message, advanced checkpoint past unprocessed). Recovery: targeted re-dispatch from outbox.
- **Owner-side drift** — source of truth itself rolled back/corrupted (e.g. DB restore). No automated recovery; operator playbook only.

**v1 toolkit:**
- **Outbox is the durable log.** Service Bus is transport. Events live in per-context outbox tables for **at least 90 days**; older entries archived to blob storage (cold) and pruned.
- **Replay tooling ships in v1.** Per-context operator-only endpoint takes `(consumer_name, from_outbox_id, to_outbox_id)` and re-dispatches. Audit-logged.
- **Targeted rebuild.** Per-Integrator, per-projection truncate + replay from `outbox.id = 0` for affected sources.
- **Idempotency is a test invariant**, not just a convention. Every consumer must carry a property test: applying the same event twice is a no-op against an already-applied state.
- **Snapshot mechanism stays v2.** New post-launch contexts bootstrap from the 90-day outbox window.
- **Owner-side drift** has no self-healing path — playbook in `docs/runbooks/source-rollback.md`. Database restores are a high-context human decision.

**Implications threaded back into iteration scope:**
- Iteration 03's outbox issue [01-outbox-relay.md](01-outbox-relay.md) gains a replay-support acceptance criterion.
- The 90-day retention + cold archive decision is recorded as its own ADR, not buried inside ADR-0014.
