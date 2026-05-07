---
id: ADR-0016
type: adr
state: accepted
title: Event-driven projections for cross-context reads
date: 2026-05-07
deciders: [kim]
---

# ADR-0016 — Event-driven projections for cross-context reads

## Context

Saasy's bounded contexts need to read each other's data:

- `SubscriptionLifecycleService` needs Customer's Currency (Tenancy) and PlanVersion's Currencies (Catalog).
- `RollupService` needs Subscription's current period and ProductType's Dimensions.
- `InvoiceComputationService` needs Subscription, PlanVersion, and Rollups to compute Line Items.

Three shapes were considered:

- **(A) Direct Repository reference.** Synchronous, immediately consistent. Couples Domains across contexts.
- **(B) Anti-corruption ports per consuming context.** Synchronous; still couples at Infrastructure.
- **(C) Event-driven projections.** Each consuming context maintains a local read model, kept in sync by subscribing to **state-change Domain Events** on the Internal Domain Event Bus ([ADR-0008](ADR-0008-internal-domain-event-bus.md)). Cross-context Domain dependencies drop to zero. Eventual consistency is the price.

Saasy explicitly values bounded-context isolation, future per-region/per-context deployments, and event-driven thinking — (C) is the chosen shape. Eventual-consistency cost is mitigated by HTTP-level retry and UI design that embraces propagation lag.

## Decision

### Cross-context reads from local projections

Every consuming context maintains its own **projection tables** in its own schema, populated by subscribing to upstream contexts' state-change events. The Domain layer of every context references at most:

- Its own Domain (always).
- `Saasy.SharedKernel.Domain` (for `Money`, `Currency`).

It does NOT reference any other context's Domain. The bus event schema is the inter-context contract.

### Cross-context IDs use local typed wrappers per consuming context

Tenancy's `CustomerId` and Subscriptions' `CustomerId` are distinct types — they never cross context Domain boundaries directly, only as `Guid` primitives in bus payloads, wrapped to the local type at consumption. ~3 lines per (foreign ID, consuming context) pair. [ADR-0013](ADR-0013-shared-kernel.md)'s exclusion of Strongly-Typed IDs from the Shared Kernel stands.

### Bus event schema: rich state-change events

Every aggregate state transition that downstream contexts mirror emits a state-change event carrying the full integration-shape snapshot of the aggregate at its new version. Examples:

- Tenancy: `IntegratorCreated`, `IntegratorTimezoneChanged`, `CustomerCreated`, `CustomerBillingAddressChanged`, `ApiKeyIssued`, …
- Catalog: `ProductTypeCreated`, `DimensionAdded`, `PlanCreated`, `PlanVersionCreated`, …
- Subscriptions: `SubscriptionCreated`, `SubscriptionTransitioned`, `ThresholdAdded`, …
- Metering: `RollupUpdated`, `UsageThresholdCrossed`.
- Invoicing: `InvoiceGenerated`, `CreditNoteIssued`.

Each payload carries: full integration-shape state, `Version` (for projection idempotency), `IntegratorId` (for bus session keying), `OccurredAt`. Projection updates are idempotent: if stored Version `>=` event Version, the event is acknowledged but the projection is unchanged.

The events serve both reactive logic (e.g., `WebhookTargetMatcher`, `RollupCatchupHandler`) and projection updates. One stream, both purposes.

### Projection update mechanics

Each consuming context registers Service Bus subscriptions on relevant upstream topics. A `ProjectionUpdater` BackgroundService per (consuming context, upstream topic) consumes events and applies them. Pattern mirrors the per-context Outbox Relay from [ADR-0008](ADR-0008-internal-domain-event-bus.md): shared base `ProjectionUpdaterBase<TEvent, TProjection>`, one concrete derived class per (context, projection).

Failure modes:
- **Bus message corruption** → dead-lettered; alerts ops.
- **Projection update DB failure** → message NOT acked; redelivered up to retry policy.
- **Out-of-order delivery** → prevented by session keying on `integrator_id`.
- **Idempotency on retry** → Version-based check.

### Write-path validation against projections

Domain Services that previously needed cross-context reads consume projection-backed ports defined in their OWN context. Example:

```csharp
// Saasy.Subscriptions.Domain/Ports/ICustomerLookup.cs
public interface ICustomerLookup
{
    Task<CustomerLookup?> GetByIdAsync(CustomerId id, CancellationToken ct = default);
}

public sealed record CustomerLookup(CustomerId Id, Guid IntegratorId, Currency Currency, uint Version);
```

The adapter (`Saasy.Subscriptions.Infrastructure.Projections.CustomerProjectionRepository`) queries Subscriptions-owned projection tables. `SubscriptionLifecycleService` takes `ICustomerLookup`, NOT `ICustomerRepository` (which is Tenancy's port).

### API-level eventual consistency contract

When write-path validation depends on a projection that hasn't caught up:

- **HTTP** `425 Too Early`
- **Header** `Retry-After: 1`
- **Body** `{ "error": { "code": "dependency.not_yet_propagated", "message": "...", "retry_after_seconds": 1 } }`

The body-level `code` is stable; SDKs hide retry behind a normal call signature. Typical propagation lag: sub-second to a few seconds.

### POST/PUT/DELETE responses serve canonical state

Write endpoints return the full canonical state of what was written, served from the writing context (source of truth, not a projection). `POST /v1/customers` returns the full Customer, so the Admin Dashboard can display it optimistically while the list endpoint (projection-backed) catches up.

### UI consequences

- Optimistic display from POST response bodies.
- Optional "Syncing…" indicators where a row's projection version is older than the latest known event.
- `Retry-After` honored on 425s — typically transparent via SDKs.
- Real-time event subscriptions for live displays (Customer Portal Rollup vs Quota) deferred to v2.

### v1 bootstrap

v1 deploys all contexts together. Every aggregate's first state-change event flows on the bus from creation; projections build up naturally. Snapshot/replay (for adding a context post-launch, recovering from extended downtime exceeding bus retention, or rebuilding a corrupted projection) is **deferred** to a future ADR — likely a per-producing-context "stream me your current state in event form" endpoint.

## Consequences

### Wins

- **True bounded-context isolation.** No cross-context Domain references. Refactoring a context's internal shape cannot break another context's compilation.
- **Future-proof for context split.** Per-context databases or separate deployable services need no Domain changes.
- **The bus is the published language.** Aggregate internals are private.
- **UI eventual-consistency design is principled.** The architecture acknowledges propagation lag; UI patterns flow from the model.
- **One pattern subsumes reactive + query.** Bus events drive reactive Domain Services AND projection updates.

### Costs

- **Eventual consistency is real.** A Customer at T+0 may not be visible to Subscriptions until T+0.5–2s. Documented; surfaced as 425.
- **Projection storage duplication.** Each consuming context stores its own copy of upstream data it needs.
- **Bus event catalog grows.** Every aggregate state transition emits an event. Schema needs careful versioning per [patterns.md](../architecture/patterns.md) §25.
- **`ProjectionUpdater` workers per consuming context.** More BackgroundServices, more bus subscriptions.
- **Replay/snapshot mechanism is deferred work.** Real cost when v2 needs it.
- **Validation invariants distributed.** Tenancy validates Currency at write time; consumer projections trust the bus payload. Mitigated by payload validation at projection-update time and integration-shape serialization happening in the producing context's interceptor (where validated Domain types are available).

### Out of scope

Concrete projection schemas; real-time UI event subscriptions; snapshot / replay; cross-context query optimization and consistency.
