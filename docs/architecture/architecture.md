# Saasy Architecture

DDD building blocks (Aggregates, Value Objects, Domain Events, Domain Services), four-layer onion (Domain → Application → Infrastructure → Api/Worker hosts), and vertical slices in the Application layer. Vocabulary per [CONTEXT.md](../../CONTEXT.md).

- [Quick Reference](#quick-reference)
- [Layer Dependencies](#layer-dependencies)
- [Bounded Contexts](#bounded-contexts)
- [Aggregate Roots](#aggregate-roots) — full map in [aggregates.md](aggregates.md)
- [Entities vs Value Objects](#entities-vs-value-objects)
- [Strongly-Typed IDs](#strongly-typed-ids)
- [Cross-Aggregate References](#cross-aggregate-references)
- [Invariants](#invariants)
- [Domain Events and the Outbox](#domain-events-and-the-outbox)
- [Data Access](#data-access)
- [EF Core Mapping Strategy](#ef-core-mapping-strategy)
- [Migrations](#migrations)
- [Domain Services](#domain-services)
- [Vertical Slices](#vertical-slices)
- [Worker Hosts](#worker-hosts)
- [Testing Layers](#testing-layers)
- [External Service Dependencies](#external-service-dependencies)
- [Aspire AppHost](#aspire-apphost)

## Quick Reference

| I need to... | Put it in... |
|---|---|
| Add a business rule | Aggregate Root method or Domain Service |
| Add a use case | `{Context}/Application/{Aggregate}/{UseCase}.cs` (Vertical Slice) |
| Add an external service | Port in `{Context}/Application/Common` (or `Domain/Common` if a Domain Service consumes it); adapter in `{Context}/Infrastructure/ExternalServices` |
| Add an API endpoint | `Saasy.Api/Endpoints/{Aggregate}Endpoints.cs` |
| Register a Handler | `{Context}/Application/Extensions/ServiceCollectionExtensions.cs` |
| Commit from a Handler | `IUnitOfWork.SaveChangesAsync()` |
| Raise a Domain Event | `RaiseDomainEvent(...)` inside the Aggregate Root method |
| Make a Webhook fire | Raise the Domain Event; the Outbox + Webhook Dispatcher handle delivery |
| Test a domain invariant | `{Context}.Domain.Tests` — Conjecture `[Property]` preferred for invariants |
| Test a Handler | `{Context}.Application.Tests` — hand-written fakes |
| Test the HTTP stack | `Saasy.Api.Tests` — `WebApplicationFactory` under Aspire |

## Layer Dependencies

```
              ┌─────────────────────────────────────┐
              │       Aspire AppHost (composition)  │
              │  Orchestrates Api + Worker hosts,   │
              │  Postgres, Event Hubs, Service Bus  │
              └────────────────┬────────────────────┘
                               │ runs
        ┌──────────────────────┼──────────────────────┐
        ▼                      ▼                      ▼
┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐
│   Saasy.Api      │    │ Saasy.Workers.*  │    │ Saasy.Workers.*  │
│  HTTP endpoints  │    │  Rollup, Invoice │    │  WebhookDispatch │
│  DI composition  │    │  OutboxRelay     │    │  ProjectionUpd.  │
└────────┬─────────┘    └────────┬─────────┘    └────────┬─────────┘
         │ depends on            │                       │
         ▼                       ▼                       ▼
┌────────────────────────────────────────────────────────────────────┐
│ {Context}.Infrastructure                                            │
│  EF Core DbContext, Repository adapters, UnitOfWork,                │
│  EntityTypeConfigurations, DomainEventToOutboxInterceptor,          │
│  external-service adapters                                           │
└────────────────────────────┬───────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────────┐
│ {Context}.Application                                               │
│  Vertical Slices, Result, IUnitOfWork, application ports            │
└────────────────────────────┬───────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────────┐
│ {Context}.Domain                                                    │
│  Aggregate Roots, Child Entities, Value Objects, Domain Events,     │
│  Domain Services, Repository interfaces, projection-backed ports.   │
│  References Saasy.SharedKernel.Domain.                              │
└────────────────────────────┬───────────────────────────────────────┘
                             ▼
┌────────────────────────────────────────────────────────────────────┐
│ Saasy.SharedKernel.Domain                                           │
│  Cross-context Value Objects only (v1: Money, Currency).            │
│  Zero NuGet refs beyond BCL. References nothing.                    │
└────────────────────────────────────────────────────────────────────┘
```

Dependencies flow inward. The Domain layer has zero NuGet references beyond the BCL. Repository interfaces live in **Domain**; their EF Core implementations live in **Infrastructure**. `IUnitOfWork` and application-orchestration ports live in **Application**.

`Saasy.SharedKernel.Domain` (per [ADR-0013](../decisions/ADR-0013-shared-kernel.md)) contains only cross-context Value Objects.

The Aspire AppHost is the *runtime composition* layer — it doesn't sit "above" Api/Worker hosts in the dependency sense; it just orchestrates them as processes.

## Bounded Contexts

| Context | Owns |
|---|---|
| `Tenancy` | Integrator, Customer, Identity Mode wiring |
| `Catalog` | Product Type, Dimension, Plan, Plan Version, Pricing Components |
| `Subscriptions` | Subscription, Plan Transition, Transition Policy, Proration |
| `Metering` | Event ingestion, Rollup, Late Event handling, Threshold detection |
| `Invoicing` | Invoice, Line Item, Credit Note, Period Close |
| `Delivery` | Webhook Subscription, Webhook Dispatcher, future channel target aggregates |

Each context has its own Domain / Application / Infrastructure project trio. Cross-context references use IDs only — no navigation properties, no FK constraints across contexts.

All Domain layers reference `Saasy.SharedKernel.Domain` for `Money` and `Currency`.

**Cross-context reads use event-driven projections, not foreign Repositories** (per [ADR-0016](../decisions/ADR-0016-event-driven-projections.md)). Each consuming context maintains its own projection tables, populated by subscribing to state-change events on the Internal Domain Event Bus. Domain Services consume projection-backed ports defined in their OWN context. Race windows surface as HTTP 425 + `Retry-After`.

The full Aggregate Root map is in [aggregates.md](aggregates.md).

## Aggregate Roots

An **Aggregate** is a cluster treated as one unit for state changes. The **Aggregate Root** is the entry point and enforces invariants for the cluster.

**Rules.**
- Code outside the aggregate must never modify Child Entities directly. Child Entity factories are `internal`.
- Repositories exist only for Aggregate Roots, never for Child Entities.
- The Aggregate Root carries a `uint Version` concurrency token.
- Only the Aggregate Root raises Domain Events; Child Entities never do.
- Aggregate Roots reference each other by Strongly-Typed ID, never by navigation property.

**Decision flow for "Aggregate Root or Child Entity?"** Apply in order:
1. **Independent reference?** Other parts refer to it directly by ID → Aggregate Root.
2. **Independent modification?** Modifications could violate the parent's invariants → Child Entity.
3. **Bounded collection?** Parent could end up with thousands → promote to Aggregate Root.
4. **Concurrency contention?** Frequently modified independently of parent → Aggregate Root.

Common case: Plan and Plan Version are **separate** Aggregate Roots even though Plan Versions are produced by editing Plans, because Subscriptions reference Plan Versions independently.

## Entities vs Value Objects

The distinction is **identity**.

- **Entity** — has a unique ID; two entities with different IDs are distinct even if all attributes match.
- **Value Object** — defined entirely by attributes. C# `record` for structural equality.

Value Objects self-validate via static `Create()` factories with private constructors:

```csharp
public sealed record Money
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    private Money(decimal amount, Currency currency) { Amount = amount; Currency = currency; }

    public static Money Create(decimal amount, Currency currency)
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative.");
        return new Money(amount, currency);
    }
}
```

`Money` and `Currency` live in `Saasy.SharedKernel.Domain` per [ADR-0013](../decisions/ADR-0013-shared-kernel.md). Test code uses `Conjecture.Money` Strategies; production code does not depend on Conjecture.

## Strongly-Typed IDs

Each Aggregate Root has its own `readonly record struct` ID:

```csharp
public readonly record struct IntegratorId(Guid Value)
{
    public static IntegratorId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
```

The compiler prevents passing a `CustomerId` where an `IntegratorId` is expected. Zero-cost — stack-allocated, structurally equal.

EF Core maps via `HasConversion`.

## Cross-Aggregate References

Aggregates reference each other **by ID only** — never by navigation property, never with FK constraints in Postgres.

```csharp
public CustomerId CustomerId { get; private init; }
public PlanVersionId PlanVersionId { get; private init; }
// NOT: public Customer Customer { get; }
```

**Why no navigation properties.** Aggregate boundaries dissolve into an object graph if loading one silently pulls in another. Lazy loading creates hidden I/O. `SaveChanges` has unbounded blast radius. Tests need to construct full graphs.

**Why no cross-aggregate FK constraints.** FKs make aggregates inseparable at the database level (cascade rules, migration ordering, blocking deletes). Saasy adds **indexes** on cross-aggregate ID columns for query performance, but no FKs. Referential integrity is enforced at the domain level (a Domain Service or Handler validates before linking).

**Snapshotting.** When data from another aggregate must persist with the consuming aggregate (e.g., a Line Item needs the Plan Version's Pricing Component values at issuance), snapshot the relevant fields at write-time. Later changes to the source aggregate don't affect existing snapshots. Right pattern for Invoices.

## Invariants

| Invariant | Enforced In |
|---|---|
| Currency must be valid ISO 4217 | `Currency` factory |
| Money amounts cannot be negative on construction | `Money.Create()` |
| Money operations require matching Currency | `Money.Add()`, `Money.Subtract()` |
| Plan Version is immutable once created | No mutating methods on `PlanVersion` |
| Subscription's Customer Currency must match its Plan Version's pricing | Subscription factory + Plan Transition |
| Quota cannot be negative | `Quota.Create()` |
| Tier ladders must be ordered, non-overlapping | Plan Version composition validation |
| Late Events outside the Late Event Window are rejected | Metering ingestion |
| Once an Invoice is Issued, it cannot be edited | No mutating methods after `Issue()` |
| Outbox Messages persist atomically with the originating aggregate change | `DomainEventToOutboxInterceptor` |

Private constructors + static `Create()` factories guarantee no Aggregate Root or Value Object can be created in an invalid state. State-transition methods include guard clauses (`EnsureDraft()`, `EnsureNotIssued()`).

## Domain Events and the Outbox

Domain Events decouple "what happened" from "what to do about it." Saasy's Domain Events drive **Webhook delivery** — a critical-must-not-be-lost side effect — via the **transactional outbox pattern** + **Internal Domain Event Bus** (per [ADR-0008](../decisions/ADR-0008-internal-domain-event-bus.md)).

### Lifecycle

1. **DEFINE** (Domain). Marker `IDomainEvent { DateTime OccurredOn { get; } }`. Concrete records: `UsageThresholdCrossed`, `InvoiceGenerated`, `SubscriptionTransitioned`, `CustomerCreated`, …
2. **RAISE** (Domain). Inside Aggregate Root methods: `RaiseDomainEvent(InvoiceGenerated.Create(Id, time))`. No I/O.
3. **COLLECT** (Domain). `AggregateRoot<TId>` stores events in a private list, exposed read-only via `IHasDomainEvents.DomainEvents`.
4. **INTERCEPT** (Infrastructure). `DomainEventToOutboxInterceptor` runs at `SavingChangesAsync` (pre-commit) on each context's `DbContext`. For each tracked `IHasDomainEvents` aggregate: serialize each Domain Event into integration shape, INSERT one row into THIS context's `outbox_messages`, clear events from the aggregate. The interceptor never reads any other context's tables.
5. **PERSIST** (Infrastructure). EF Core commits the aggregate change AND the outbox row in one transaction.
6. **RELAY** (per-context Outbox Relay BackgroundService). Tails its context's `outbox_messages WHERE relayed_at IS NULL`, publishes each row to that context's topic on the Internal Domain Event Bus (`saasy.domain-events.{context}`), sets `relayed_at` on success. MessageId = `outbox_messages.id`; SessionId = `integrator_id` for per-Integrator FIFO. Topic-level duplicate detection catches relay-restart republishes.
7. **DISPATCH** (Channel Dispatcher). Webhook Dispatcher subscribes to every `saasy.domain-events.*` topic. Per message: look up active `WebhookSubscription`s whose `SubscribedKinds` include the event kind, INSERT `outbox_dispatch (outbox_message_id, channel='webhook', target_id=...) ON CONFLICT DO NOTHING`, complete the message.
8. **DELIVER** (Channel Dispatcher). For each unprocessed dispatch row: sign with the WebhookSubscription's secret, POST to the Integrator's URL. Mark `processed_at` on success / bump `attempt_count` and `available_at` on transient failure / set `dead_lettered_at` after max attempts.

### Why pre-commit insertion

Pre-commit makes the outbox row atomic with the aggregate change. If the transaction rolls back (concurrency conflict), no orphan outbox message exists. Cost: delivery is out-of-band — the Webhook arrives seconds after the API response.

### Why per-context outbox tables

A single shared `outbox_messages` table would force every context's interceptor to write across schemas (or databases). Each context owns its outbox table, written only by its own DbContext, in the same transaction as its own aggregate changes.

### Why fan-out at the Dispatcher (not the interceptor)

Earlier drafts had the interceptor look up matching WebhookSubscriptions and write one outbox row per match. That required every emitting context's interceptor to read Delivery-context tables — same coupling, in reverse. Fan-out at the Dispatcher keeps each interceptor scoped to its own context, and a WebhookSubscription created between event raise and dispatch correctly receives the event.

### Domain Event naming

Past tense: `UsageThresholdCrossed`, `InvoiceGenerated`, `SubscriptionTransitioned`. Mapped to Webhook kinds via a fixed table maintained in the Delivery context.

## Data Access

**Repositories exist only for Aggregate Roots.** One Repository interface per Aggregate Root, defined in Domain; one EF Core implementation per Repository, in Infrastructure.

```csharp
// Saasy.Subscriptions.Domain/ISubscriptionRepository.cs
public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken ct = default);
    Task<bool> ExistsAsync(SubscriptionId id, CancellationToken ct = default);
    Task<IReadOnlyList<Subscription>> ListForCustomerAsync(CustomerId customerId, CancellationToken ct = default);
    void Add(Subscription subscription);
}
```

**Conventions.**
- `Add()` is synchronous (in-memory tracking). Persistence happens via `IUnitOfWork.SaveChangesAsync()`.
- Read-list queries use `AsNoTracking()`.
- Single-entity loads (`GetByIdAsync`) are tracked because Handlers mutate.
- Cross-aggregate ID parameters (`ExistsByCustomerIdAsync`) are fine — they accept Strongly-Typed IDs but never load or return foreign aggregates.

### Unit of Work

```csharp
public interface IUnitOfWork { Task SaveChangesAsync(CancellationToken ct = default); }

public sealed class UnitOfWork({Context}DbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException ex) { throw new ConcurrencyException("...", ex); }
        catch (DbUpdateException ex) { throw new PersistenceException("...", ex); }
    }
}
```

`UnitOfWork` is the only place EF Core exception types are mentioned. Application Handlers catch `ConcurrencyException` / `PersistenceException` and translate to `Result.Conflict(...)` / `Result.Failure(...)`.

### Optimistic Concurrency

Every Aggregate Root carries `uint Version`. `{Context}DbContext.SaveChangesAsync` bumps it on every modified Aggregate Root before persisting. EF Core's `IsConcurrencyToken()` adds `WHERE Version = @expected` to UPDATEs; mismatches throw `DbUpdateConcurrencyException`.

Child Entity changes mark the parent Aggregate Root as `Modified` (via `OwnsMany`), so the Version bumps even if no field on the root itself changed. This is correct: the aggregate as a whole was modified.

API responses include `Version`; `If-Match` header support is a future enhancement.

## EF Core Mapping Strategy

Saasy persists to PostgreSQL via EF Core + Npgsql.

| DDD Concept | EF Core | Postgres |
|---|---|---|
| Strongly-Typed ID | `HasConversion(id => id.Value, guid => new XId(guid))` | `uuid` |
| Single-property VO | `HasConversion(vo => vo.Value, raw => XValue.Create(raw))` | column type per VO; re-validates on read |
| Multi-property VO | `OwnsOne(...)` with column prefixes | inline columns |
| Child Entity collection | `OwnsMany(...).ToTable(...)` with `WithOwner().HasForeignKey(...)` | separate table; cascade-deleted with parent |
| Enum | `HasConversion<string>()` | `text` (`varchar(N)` for short enums) |
| Cross-aggregate reference | plain `uuid` with index, **no FK** | indexed for query, decoupled at schema level |
| Private collection backing | `Navigation(x => x.Lines).UsePropertyAccessMode(Field)` | EF Core writes to `_lines` |
| Computed property | `Ignore(...)` | not persisted |
| Concurrency token | `Property(x => x.Version).IsConcurrencyToken()` | `bigint` (cast from `uint`) |
| Append-only Events | Event Hubs Capture → `events_recent` table for the Late Event Window | partitioned by `(integrator_id, day)` |
| Flexible payload | `HasColumnType("jsonb")` | use sparingly; great for Event `properties{}` |

**Value Object materialization.** `HasConversion` calls `Create()` on every read, re-running validation. Acceptable for normal volumes; a separate `Reconstitute()` factory that skips validation is the escape hatch for hot read paths.

## Migrations

Real EF Core migrations (`dotnet ef migrations add ...`) — never `EnsureCreatedAsync`. Each bounded context has its own `DbContext` and its own migrations folder. Schema changes go through PR review.

Dev: migrations run automatically via the Aspire AppHost. Production: a `Saasy.Migrate` console host runs them in the deployment pipeline.

## Domain Services

Domain Services hold business logic that spans multiple Aggregate Roots or requires Repository / projection access to enforce invariants. They validate and construct but never persist; the calling Handler owns the transaction boundary.

Examples (names indicative):
- `SubscriptionLifecycleService` — Customer existence, Plan Version Currency match, Plan Transition coordination, Plan exclusivity check.
- `RollupService` — applies Aggregations across Events; recomputes Rollups when Late Events arrive.
- `InvoiceComputationService` — at Final Close, walks Pricing Components against Rollups deterministically.
- `WebhookTargetMatcher` — given a bus message, finds matching `WebhookSubscription`s. Lives in `Saasy.Delivery.Domain`; consumed by the Webhook Dispatcher.

**When Domain Service vs Aggregate Root method.** If logic only needs data already inside the aggregate, it lives on the aggregate. If it needs other aggregates or cross-context projections, it's a Domain Service.

## Vertical Slices

Each use case is one file: a `public static class` containing a nested `Command` (or `Query`) record and a nested `Handler` class.

```
Saasy.Subscriptions.Application/Subscriptions/
├── CreateSubscription.cs
├── TransitionSubscription.cs
├── CancelSubscription.cs
├── GetSubscriptionById.cs
├── ListSubscriptionsForCustomer.cs
└── SubscriptionResponse.cs
```

### Anatomy

```csharp
public static class TransitionSubscription
{
    public sealed record TransitionSubscriptionCommand(
        Guid SubscriptionId,
        Guid TargetPlanVersionId,
        TransitionEffect? EffectOverride,
        TransitionProration? ProrationOverride)
    {
        public ValidationResult Validate() { /* ... */ }
    }

    public sealed class Handler(
        SubscriptionLifecycleService lifecycle,
        ISubscriptionRepository repo,
        IUnitOfWork unitOfWork)
    {
        public async Task<Result<SubscriptionResponse>> HandleAsync(
            TransitionSubscriptionCommand command, CancellationToken ct = default)
        {
            var validation = command.Validate();
            if (!validation.IsValid) return Result<SubscriptionResponse>.ValidationFailure(validation);

            var subscription = await repo.GetByIdAsync(new SubscriptionId(command.SubscriptionId), ct);
            if (subscription is null) return Result<SubscriptionResponse>.NotFound("Subscription");

            try
            {
                await lifecycle.TransitionAsync(subscription, new PlanVersionId(command.TargetPlanVersionId),
                    command.EffectOverride, command.ProrationOverride, ct);
                await unitOfWork.SaveChangesAsync(ct);
                return SubscriptionResponse.FromDomain(subscription);
            }
            catch (ConcurrencyException) { return Result<SubscriptionResponse>.Conflict("..."); }
        }
    }
}
```

### Conventions

- One file per use case; the outer class is `static` (a namespace).
- Command records have `Validate()`. Query records do not.
- Handlers are concrete classes — no `ICommandHandler<,>`. Registered as `TransitionSubscription.Handler`.
- No mediator, no pipeline, no decorators in v1. Cross-cutting concerns are explicit in the Handler.
- Handlers always commit via `IUnitOfWork.SaveChangesAsync()`. Queries don't take a UnitOfWork.
- Commands return `Result<TResponse>`; queries return DTO or `null`.

## Worker Hosts

| Host | Role |
|---|---|
| `Saasy.Workers.RollupWorker` | Consume Events, apply Aggregations, persist Rollups, raise `UsageThresholdCrossed`. |
| `Saasy.Workers.InvoiceGenerator` | At Final Close, compute Invoices, raise `InvoiceGenerated`. |
| `Saasy.Workers.OutboxRelay` | One `BackgroundService` per emitting context, derived from `OutboxRelayBase<TSource>` in `Saasy.Outbox.Infrastructure`; tails its context's `outbox_messages` and publishes to the bus. |
| `Saasy.Workers.WebhookDispatcher` | Subscribes to every `saasy.domain-events.*` topic; writes `outbox_dispatch` rows; signs and POSTs payloads. |
| `Saasy.Workers.OutboxRetention` | Prunes outbox rows whose dispatch state is terminal across every live channel and older than the retention window. |
| `Saasy.Workers.ProjectionUpdater` | One `BackgroundService` per (consuming context, upstream topic), derived from `ProjectionUpdaterBase<TEvent, TProjection>`; applies state-change events to projection tables. Idempotent via Version. ([ADR-0016](../decisions/ADR-0016-event-driven-projections.md)) |

Each Worker host has its own `Program.cs` calling `AddApplication()` and `AddInfrastructure()` for the contexts it consumes. Aspire-orchestrated in dev; deployed as its own Container App / process in production.

Worker code does NOT live in the Api project. Workers and Api are siblings, both depending on Application + Infrastructure.

## Testing Layers

| Test Project | Depends On | Tests | Strategy |
|---|---|---|---|
| `{Context}.Domain.Tests` | Domain only | Aggregate Roots, VOs, Domain Services | Conjecture `[Property]` for invariants; plain construction for examples; `Conjecture.Money` / `Conjecture.Time` |
| `{Context}.Application.Tests` | Application + Domain | Handlers | Hand-written fakes for Repositories and ports; `Conjecture.Interactions` for state-machine sequences |
| `Saasy.Api.Tests` | Api (full stack) | HTTP endpoints | `WebApplicationFactory` with Postgres test container under Aspire; `Conjecture.AspNetCore` + `Conjecture.OpenApi` |
| `{Context}.Infrastructure.Tests` | Infrastructure | EF Core mappings, Repository round-trips | `Conjecture.EFCore` against a real Postgres test container |

PBT defaults per [ADR-0005](../decisions/ADR-0005-property-based-testing.md). Shared test fakes live in `{Context}.TestHelpers/` projects when consumed by both Domain and Application tests; project-local fakes in a `TestHelpers/` folder.

## External Service Dependencies

Saasy integrates with external systems via the **anti-corruption layer** pattern: a port owned by an inner layer, an adapter in Infrastructure. ACL is for **external** boundaries only — inter-context decoupling uses event-driven projections per [ADR-0016](../decisions/ADR-0016-event-driven-projections.md), not ACL.

| Question | Answer |
|---|---|
| Domain Service needs the dependency to enforce a business rule? | Port in Domain; domain types at the boundary |
| Application Handler orchestrating a workflow step? | Port in Application; primitives or DTOs at the boundary |
| Dependency needs Child Entity data? | Purpose-built DTO; never expose Child Entities |

Examples expected:
- `IEventHubPublisherCredentialIssuer` (Application) — per-Integrator SAS tokens.
- `IInvoiceRenderer` (Application) — Invoice → JSON + HTML.
- `ICurrencyConverter` (Domain if introduced) — Saasy does not convert in v1, but if a future Domain Service needs FX validation, the port lives in Domain.
- `IPaymentGateway` (Application, future) — primitives at the boundary.

## Aspire AppHost

`Saasy.AppHost` composes the runtime topology:

- **Services**: `Saasy.Api`, each `Saasy.Workers.*`, `Saasy.Migrate`.
- **Resources**: Postgres (Flexible Server in production; container in dev), Event Hubs (real namespace in production; emulator in dev), Service Bus, Azure Storage (for Event Hubs Capture).
- **Telemetry**: OpenTelemetry pipeline; Aspire dashboard in dev; OTel exporter to the production observability sink (future ADR).
- **Configuration**: connection strings via Aspire's parameter system.

Dev: `dotnet run --project Saasy.AppHost`. Production: `aspirate generate` (or equivalent) produces deployment artifacts (Container App definitions, Helm charts, etc.) — deployment-topology ADR pending.

The AppHost is a thin composition root. Business logic lives in the four-layer onion below it.
