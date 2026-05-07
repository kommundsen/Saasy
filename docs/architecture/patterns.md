# Saasy DDD Pattern Catalog

Reference for the patterns Saasy uses, with concrete examples. Vocabulary per [CONTEXT.md](../../CONTEXT.md). Architectural overview in [architecture.md](architecture.md). Aggregate map in [aggregates.md](aggregates.md).

---

## 1. Static Factory Methods

Private constructors paired with static `Create()` methods that validate input and return always-valid objects. Constructors can't return null or fail gracefully and allow partially-initialized objects; static factories are the single entry point where invariants are enforced.

```csharp
public sealed record Money
{
    private Money(decimal amount, Currency currency) { Amount = amount; Currency = currency; }

    public static Money Create(decimal amount, Currency currency)
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative.");
        return new Money(amount, currency);
    }
}
```

Aggregate Root factories produce fully formed aggregates ready to persist. Child Entity factories are `internal` so only the owning aggregate can construct them.

---

## 2. Aggregate Root

A cluster of domain objects treated as one consistency unit. The root carries `uint Version`, owns Child Entities, and raises Domain Events. Aggregates define transactional boundaries: saved atomically, concurrent modifications detected via the Version token.

```csharp
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public uint Version { get; private set; }

    protected void RaiseDomainEvent(IDomainEvent e) => _domainEvents.Add(e);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

Saasy's Aggregate Roots are mapped in [aggregates.md](aggregates.md). **Subscription** owns ThresholdConfig as a small Child Entity collection; **Invoice** owns Line Items and is immutable once Issued; **PlanVersion** is fully immutable; **WebhookSubscription** is a regular Aggregate Root (no claim concurrency — fan-out happens at the Dispatcher when consuming bus messages).

---

## 3. Entity

Defined by identity. Two Entities with the same ID are equal regardless of attribute differences. Models things with lifecycles tracked across time (Subscriptions, Invoices).

```csharp
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected init; } = default!;

    public override bool Equals(object? obj) =>
        obj is Entity<TId> other && GetType() == other.GetType()
        && !EqualityComparer<TId>.Default.Equals(Id, default!)
        && Id.Equals(other.Id);

    public override int GetHashCode() =>
        EqualityComparer<TId>.Default.Equals(Id, default!)
            ? RuntimeHelpers.GetHashCode(this)
            : Id.GetHashCode();
}
```

---

## 4. Value Objects

Immutable, defined entirely by attributes, no identity. C# `record` for structural equality. Eliminates primitive obsession; centralizes validation; safe to share.

`Money` and `Currency` live in `Saasy.SharedKernel.Domain` (per Pattern 24 / [ADR-0013](../decisions/ADR-0013-shared-kernel.md)). Per-context VOs (`Address`, `Slug`, `IntegratorKind`, `IntegratorTier`, `BillingPeriod`, `TransitionPolicy`, `TierLadder`) live in their owning context's `Domain.Common`. All `sealed record`. All self-validating via `Create()`.

```csharp
public sealed record Currency
{
    public string Code { get; }
    private Currency(string code) => Code = code;

    public static Currency Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 3)
            throw new ArgumentException("Currency code must be 3 letters.");
        return new Currency(code.ToUpperInvariant());
    }
}
```

---

## 5. Strongly-Typed IDs

Each Aggregate Root has its own `readonly record struct` wrapping `Guid`. The compiler prevents passing a `CustomerId` where an `IntegratorId` is expected. Stack-allocated, value-equal — zero-cost.

```csharp
public readonly record struct IntegratorId(Guid Value)
{
    public static IntegratorId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

builder.Property(o => o.Id).HasConversion(id => id.Value, guid => new IntegratorId(guid));
```

---

## 6. Domain Events

Immutable records describing something significant that happened. Raised inside Aggregate Root methods; collected on the aggregate; written to the originating context's **per-context Outbox** (Pattern 21) atomically with the aggregate change; relayed to the Internal Domain Event Bus; consumed by channel-specific Dispatcher workers.

```csharp
public interface IDomainEvent { DateTime OccurredOn { get; } }

public sealed record InvoiceGenerated(InvoiceId InvoiceId, IntegratorId IntegratorId, DateTime OccurredOn)
    : IDomainEvent
{
    public static InvoiceGenerated Create(InvoiceId id, IntegratorId integratorId, TimeProvider time) =>
        new(id, integratorId, time.GetUtcNow().UtcDateTime);
}
```

```csharp
public void Issue(TimeProvider timeProvider)
{
    EnsureDraft();
    Status = InvoiceStatus.Issued;
    IssuedAt = timeProvider.GetUtcNow().UtcDateTime;
    RaiseDomainEvent(InvoiceGenerated.Create(Id, IntegratorId, timeProvider));
}
```

Past-tense names: `InvoiceGenerated`, `UsageThresholdCrossed`, `SubscriptionTransitioned`, `CustomerCreated`. Mapped to Webhook kinds via a fixed table maintained in the Delivery context.

---

## 7. Repository

Collection-like interface for one Aggregate Root. Defined in Domain (port); implemented with EF Core in Infrastructure (adapter). One Repository per Aggregate Root, never per Child Entity.

```csharp
// Domain port
public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken ct = default);
    Task<bool> ExistsAsync(SubscriptionId id, CancellationToken ct = default);
    void Add(Subscription subscription);
}
```

`Add()` is synchronous. Persistence happens via `IUnitOfWork.SaveChangesAsync()`.

---

## 8. Unit of Work

Application-layer abstraction over the transactional boundary. Handlers commit via `IUnitOfWork.SaveChangesAsync()`. The implementation translates EF Core exceptions to Application-layer types so Application code stays free of ORM types.

```csharp
public interface IUnitOfWork { Task SaveChangesAsync(CancellationToken ct = default); }

public sealed class UnitOfWork(SubscriptionsDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException ex) { throw new ConcurrencyException("...", ex); }
        catch (DbUpdateException ex) { throw new PersistenceException("...", ex); }
    }
}
```

---

## 9. Domain Services

Stateless services holding business logic that spans multiple Aggregate Roots or requires Repository / projection access. They validate and construct; never persist.

**Cross-context reads use local projections, not foreign Repositories** (per [ADR-0016](../decisions/ADR-0016-event-driven-projections.md)). Domain Services consume projection-backed ports defined in their OWN context — never a foreign context's Repository:

```csharp
// Saasy.Subscriptions.Domain/Ports/ICustomerLookup.cs
public interface ICustomerLookup
{
    Task<CustomerLookup?> GetByIdAsync(CustomerId id, CancellationToken ct = default);
}

public sealed record CustomerLookup(CustomerId Id, Guid IntegratorId, Currency Currency, uint Version);

// Saasy.Subscriptions.Domain/SubscriptionLifecycleService.cs
public sealed class SubscriptionLifecycleService(
    ICustomerLookup customerLookup,             // projection-backed
    IPlanLookup planLookup,                     // projection-backed
    IPlanVersionLookup planVersionLookup,       // projection-backed
    ISubscriptionRepository subscriptionRepo,   // source-of-truth
    TimeProvider timeProvider)
{
    public async Task<Result<Subscription>> CreateAsync(
        CustomerId customerId, PlanVersionId planVersionId, CancellationToken ct = default)
    {
        var customer = await customerLookup.GetByIdAsync(customerId, ct);
        if (customer is null) return Result<Subscription>.NotYetPropagated("Customer");

        var planVersion = await planVersionLookup.GetByIdAsync(planVersionId, ct);
        if (planVersion is null) return Result<Subscription>.NotYetPropagated("PlanVersion");

        if (!planVersion.SupportsCurrency(customer.Currency))
            return Result<Subscription>.Failure(
                $"PlanVersion {planVersionId} does not price {customer.Currency.Code}.");

        var plan = await planLookup.GetByIdAsync(planVersion.PlanId, ct);
        if (plan is { IsExclusivePerCustomer: true } &&
            await subscriptionRepo.HasActiveSubscriptionForPlanAsync(customerId, plan.Id, ct))
            return Result<Subscription>.Conflict("Plan is exclusive; Customer already has an active Subscription.");

        return Subscription.Create(customerId, planVersionId, customer.Currency, timeProvider);
    }
}
```

`NotYetPropagated` maps to HTTP 425 + `Retry-After`. `Saasy.Subscriptions.Domain.csproj` references only `Saasy.SharedKernel.Domain` — no Tenancy/Catalog Domain references.

| Context | Service | Cross-context inputs (via projections) |
|---|---|---|
| Tenancy | `IntegratorProvisioningService` | — |
| Catalog | `PlanVersioningService` | Integrator |
| Subscriptions | `SubscriptionLifecycleService` | Customer, Plan, PlanVersion |
| Metering | `RollupService` | Subscription, ProductType (Dimensions) |
| Invoicing | `InvoiceComputationService` | Subscription, PlanVersion, Rollup, Customer, Integrator |
| Delivery | `WebhookTargetMatcher` | — (consumes bus events directly) |

---

## 10. CQRS (Manual)

Commands change state; Queries read state. Separate Vertical Slices. Handlers are concrete classes registered in DI directly — no mediator library.

```csharp
public static class GetSubscriptionById
{
    public sealed record GetSubscriptionByIdQuery(Guid Id);

    public sealed class Handler(ISubscriptionRepository repo)
    {
        public async Task<SubscriptionResponse?> HandleAsync(
            GetSubscriptionByIdQuery query, CancellationToken ct = default)
        {
            var subscription = await repo.GetByIdAsync(new SubscriptionId(query.Id), ct);
            return subscription is null ? null : SubscriptionResponse.FromDomain(subscription);
        }
    }
}
```

Queries don't depend on UnitOfWork or Domain Services.

---

## 11. Result

Discriminated outcome type returned by Command Handlers: `Success`, `Failure`, `NotFound`, `Conflict`, `ValidationFailure`, `NotYetPropagated`. Business failures (Customer not found, Plan Transition invalid) are expected outcomes — not exceptions.

```csharp
public sealed class Result<T> : IAppResult
{
    public T? Value { get; }
    public string? Error { get; }
    public bool IsSuccess { get; }
    public bool IsNotFound { get; }
    public bool IsConflict { get; }
    public bool IsNotYetPropagated { get; }
    public IReadOnlyList<ValidationError>? Errors { get; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);
    public static Result<T> NotFound(string entity = "Resource") => new($"{entity} not found.", isNotFound: true);
    public static Result<T> Conflict(string error) => new(error, isConflict: true);
    public static Result<T> ValidationFailure(ValidationResult validation) => new(validation.Errors);
    public static Result<T> NotYetPropagated(string entity) => new($"{entity} not yet propagated.", isNotYetPropagated: true);
}
```

Api maps each variant to the right HTTP status (Conflict → 409, NotYetPropagated → 425, etc.).

---

## 12. Anti-Corruption Layer

Interfaces that insulate the Domain from **external systems** by using primitive types or domain-neutral DTOs at the boundary. External systems change independently; Saasy's Domain stays stable.

**ACL is for external boundaries, not inter-context boundaries.** Inter-context decoupling is achieved via event-driven projections ([ADR-0016](../decisions/ADR-0016-event-driven-projections.md)). The bus event schema is the inter-context contract; there are NO consuming-context Domain references to producing-context Domains, and NO ACL-style ports between contexts.

ACL applies to external systems Saasy does not control — Event Hubs SDK, Service Bus SDK, future payment gateways, future tax calculators, future external Service Bus / Event Hub delivery to Integrator-owned namespaces.

| Port | Layer | Boundary types |
|---|---|---|
| `ICurrencyConverter` (if introduced) | Domain | Domain types (`Money`) |
| `IPaymentGateway` (future) | Application | Primitives (`Guid`, `decimal`, `string`) |
| `IInvoiceRenderer` | Application | Purpose-built DTO |
| `IEventHubPublisherCredentialIssuer` | Application | Primitives |

---

## 13. Guard Clauses

Validation at entity boundaries that throws immediately on invalid input. Factory methods validate construction; state-transition methods guard against illegal operations.

```csharp
private void EnsureDraft()
{
    if (Status != SubscriptionStatus.Draft)
        throw new InvalidOperationException($"Cannot modify a subscription in '{Status}' status.");
}

public void Transition(PlanVersionId target, TimeProvider time)
{
    EnsureNotCancelled();
    if (target == PlanVersionId)
        throw new InvalidOperationException("Already on target PlanVersion.");
    // ...
}
```

If invalid state can't be constructed, no defensive checks are needed elsewhere. Saasy's invariant table is in [architecture.md](architecture.md#invariants).

---

## 14. Optimistic Concurrency

Every Aggregate Root carries `uint Version`. EF Core configures it as `IsConcurrencyToken()`. On UPDATE, the database checks the version hasn't changed since load.

```csharp
builder.Property(s => s.Version).IsConcurrencyToken();

// In SaveChangesAsync override:
foreach (var entry in ChangeTracker.Entries())
    if (entry is { Entity: IHasDomainEvents, State: EntityState.Modified })
        entry.Property(nameof(AggregateRoot<int>.Version)).CurrentValue =
            (uint)entry.Property(nameof(AggregateRoot<int>.Version)).CurrentValue! + 1;
```

`UnitOfWork` translates `DbUpdateConcurrencyException` to `ConcurrencyException`; Handlers map to `Result.Conflict(...)`; Api maps to HTTP 409.

API responses include `Version` for client-side optimistic concurrency (`If-Match` support is a future addition).

---

## 15. State Machine

Enum representing finite states with transitions enforced by Aggregate Root methods.

```
Subscription:
  Draft → Active → Cancelled (terminal)
            │
            ├→ Paused → Active
            │     └→ Cancelled
            └→ Cancelled

Invoice:
  Draft → Issued (terminal — only Credit Notes can adjust)

PlanVersion: (no states — immutable)

Plan:
  Draft → Published → Archived (terminal)

OutboxDispatch (per channel × target × outbox message):
  Unclaimed → InFlight → Processed (terminal)
                  │   └→ failure → Unclaimed (with backoff via available_at)
                  └→ MaxAttempts → DeadLettered (terminal)
  (OutboxMessage itself has no state machine — immutable record once written;
   relayed_at flips once when the Outbox Relay publishes.)

WebhookSubscription:
  Active ⇄ Paused
     └→ Revoked (terminal)
```

---

## 16. Saga / Compensating Transactions — Deferred

Multi-step operations spanning aggregates or external services where a failed later step triggers explicit undo. Saasy v1 has no payments, so the canonical saga (charge → fail → refund) doesn't apply. Will apply when payments land (Plan Transition with refund; Invoice with idempotent retries on charge failure → Credit Note compensation). Documented for awareness; concrete code arrives with payments.

---

## 17. Clean / Hexagonal Architecture

Domain at the center; Application wraps it; Infrastructure outside; Api/Worker hosts at the composition root. Dependencies point inward. Domain has zero external dependencies.

```
Saasy.SharedKernel.Domain       ← Pure C#, no NuGet refs beyond BCL, no context refs
  └── (v1: Money, Currency)

Saasy.{Context}.Domain          ← Pure C#; refs SharedKernel
  ├── Common/                   (Entity, AggregateRoot, IDomainEvent, ports)
  ├── {Aggregate1}/
  └── {Aggregate2}/

Saasy.{Context}.Application     ← Refs Domain only
  ├── Common/                   (IUnitOfWork, Result, application ports)
  ├── {Aggregate1}/             (Vertical Slices)
  └── Extensions/               (AddApplication())

Saasy.{Context}.Infrastructure  ← Refs Application + Domain
  ├── Persistence/
  ├── ExternalServices/
  └── Extensions/               (AddInfrastructure())

Saasy.Api                       ← HTTP composition root
Saasy.Workers.{Name}            ← Worker composition roots
Saasy.AppHost                   ← Aspire orchestrator
```

---

## 18. Module Pattern for DI

Each layer exposes a single `IServiceCollection` extension method (`AddApplication()`, `AddInfrastructure()`) registering its services. Composition roots call them.

```csharp
public static IServiceCollection AddSubscriptionsApplication(this IServiceCollection services)
{
    services.AddScoped<SubscriptionLifecycleService>();
    services.AddScoped<CreateSubscription.Handler>();
    services.AddScoped<TransitionSubscription.Handler>();
    return services;
}

public static IServiceCollection AddSubscriptionsInfrastructure(
    this IServiceCollection services, string connectionString)
{
    services.AddDbContext<SubscriptionsDbContext>(opts => opts.UseNpgsql(connectionString));
    services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    return services;
}
```

`Saasy.Api/Program.cs` stays minimal — one pair of calls per context.

---

## 19. EF Core Mapping

Detailed table in [architecture.md](architecture.md#ef-core-mapping-strategy). Highlights:

```csharp
// Multi-property VO
builder.OwnsOne(c => c.BillingAddress, addr =>
{
    addr.Property(a => a.Street).HasMaxLength(200).IsRequired();
    addr.Property(a => a.City).HasMaxLength(100).IsRequired();
    addr.Property(a => a.PostalCode).HasMaxLength(20).IsRequired();
    addr.Property(a => a.Country).HasMaxLength(100).IsRequired();
});

// Child Entity collection
builder.OwnsMany(i => i.Lines, line =>
{
    line.ToTable("invoice_lines");
    line.HasKey(l => l.Id);
    line.Property(l => l.Id).HasConversion(id => id.Value, guid => new LineItemId(guid));
    line.OwnsOne(l => l.UnitAmount, money =>
    {
        money.Property(m => m.Amount).HasColumnType("decimal(18,4)");
        money.Property(m => m.Currency).HasMaxLength(3);
    });
    line.WithOwner().HasForeignKey("invoice_id");
    line.UsePropertyAccessMode(PropertyAccessMode.Field);
});

builder.Property(c => c.Id).HasConversion(id => id.Value, guid => new CustomerId(guid));
builder.Property(c => c.Currency).HasConversion(c => c.Code, raw => Currency.Create(raw)).HasMaxLength(3);
builder.Property<Dictionary<string, string>>("Properties").HasColumnType("jsonb");
builder.Property(c => c.Version).IsConcurrencyToken();
builder.HasIndex(s => s.CustomerId);  // index but no FK across aggregates
```

---

## 20. Specification — Deferred

Encapsulating query predicates in reusable, composable objects. v1 uses plain LINQ in Repository methods. Reach for Specifications when methods start accumulating similar variations (`GetByStatus`, `GetByIntegratorAndStatus`, `GetByCustomerAndStatus`). Likely candidates: complex Customer queries (Tenancy), complex Subscription queries (Admin Dashboard).

---

## 21. Transactional Outbox (Per-Context) + Internal Domain Event Bus

Each emitting context owns its own `outbox_messages` table. Domain Events are written there in the same database transaction as the aggregate change. A per-context **Outbox Relay** tails the table and publishes each row to that context's topic on the **Internal Domain Event Bus** (Service Bus topics, sessions keyed by `integrator_id`). Channel-specific **Dispatcher** workers (Webhook in v1) subscribe to the bus, perform target fan-out, and write per-(message, channel, target) rows to `outbox_dispatch` tables.

Per [ADR-0008](../decisions/ADR-0008-internal-domain-event-bus.md). The transactional outbox guarantees at-least-once persistence atomic with the aggregate change. The per-context split prevents cross-context schema reach. The bus decouples emitting contexts from delivery channels.

### Schema

```sql
-- Per emitting context, e.g. invoicing.outbox_messages
CREATE TABLE outbox_messages (
    id              uuid        PRIMARY KEY,
    integrator_id   uuid        NOT NULL,
    event_kind      text        NOT NULL,
    payload_json    jsonb       NOT NULL,
    occurred_at     timestamptz NOT NULL,
    relayed_at      timestamptz NULL
);
CREATE INDEX ON outbox_messages (relayed_at, occurred_at) WHERE relayed_at IS NULL;

-- Per dispatcher channel, e.g. delivery.outbox_dispatch
CREATE TABLE outbox_dispatch (
    id                  uuid        PRIMARY KEY,
    outbox_message_id   uuid        NOT NULL,
    channel             text        NOT NULL,
    target_id           text        NOT NULL,
    claimed_at          timestamptz NULL,
    attempt_count       int         NOT NULL DEFAULT 0,
    last_attempt_at     timestamptz NULL,
    last_error          text        NULL,
    available_at        timestamptz NULL,
    processed_at        timestamptz NULL,
    dead_lettered_at    timestamptz NULL,
    UNIQUE (outbox_message_id, channel, target_id)
);
CREATE INDEX ON outbox_dispatch (processed_at, dead_lettered_at, available_at)
    WHERE processed_at IS NULL AND dead_lettered_at IS NULL;
```

### Pieces

- **`DomainEventToOutboxInterceptor`** — registered on every emitting context's `DbContext`. Runs at `SavingChangesAsync` (pre-commit). Scans `ChangeTracker` for `IHasDomainEvents` aggregates, serializes each event into integration shape, INSERTs rows into THIS context's `outbox_messages`. Never reads any other context's tables.
- **`IOutboxSource`** — per-context port. Exposes `ReadUnrelayedAsync(batchSize, ct)` and `MarkRelayedAsync(id, ct)`.
- **`OutboxRelayBase<TSource>`** — shared abstract `BackgroundService` in `Saasy.Outbox.Infrastructure`. Read → publish-to-bus → mark-relayed loop. Each emitting context has a per-context derived class (`InvoicingOutboxRelay : OutboxRelayBase<InvoicingOutboxSource>`) with `AddHostedService` registration for failure isolation and observability.
- **Internal Domain Event Bus** — Azure Service Bus, one topic per emitting context. Sessions = `integrator_id`. Topic-level duplicate detection (~10 min). Bus MessageId = `outbox_messages.id`.
- **Channel Dispatcher** (`Saasy.Workers.WebhookDispatcher`) — subscribes to every `saasy.domain-events.*` topic with sessions. On each message: looks up matching `WebhookSubscription`s, INSERTs `outbox_dispatch` rows ON CONFLICT DO NOTHING, completes the bus message. A second loop processes unprocessed dispatch rows (sign + POST + retry-with-backoff). Idempotency via the `(outbox_message_id, channel, target_id)` unique key.

### At-least-once boundaries

Three handoffs, all deliberate; downstream idempotency catches duplicates:

1. **Outbox row → bus.** Relay publishes, then marks `relayed_at`. Caught by Service Bus duplicate detection on MessageId.
2. **Bus message → dispatch row insert.** Dispatcher consumes, INSERTs, completes. Caught by `ON CONFLICT DO NOTHING`.
3. **Dispatch row → outbound HTTP.** Dispatcher claims, POSTs, marks `processed_at`. Caught by Integrator's required idempotency on `event_id`.

---

## 22. Event Sourcing — Not Adopted

Saasy uses the current-state model with optimistic concurrency. The Outbox gives an audit trail of *external* events (what was delivered to Integrators) without the complexity of full event sourcing (event schema evolution, snapshotting, replay correctness). Revisit only if Integrators ask for a complete history of every domain change Saasy made on their behalf.

---

## 23. Bounded Context / Context Mapping

Each bounded context has its own ubiquitous language and model. Saasy's contexts: Tenancy, Catalog, Subscriptions, Metering, Invoicing, Delivery. Cross-context references by ID only.

Relationships:
- **Tenancy ← every other context.** Tenancy is upstream; everything references IntegratorId / CustomerId.
- **Catalog ← Subscriptions ← Invoicing.** Subscriptions reference PlanVersionId; Invoices snapshot Pricing Component data at issue time.
- **Subscriptions → Metering.** Rollups key on SubscriptionId.
- **Every emitting context → Internal Domain Event Bus → Delivery.** Domain Events flow from each context's per-context outbox to its bus topic; Delivery's Dispatchers consume topics and produce outbound deliveries.

---

## 24. Shared Kernel

Per [ADR-0013](../decisions/ADR-0013-shared-kernel.md), `Saasy.SharedKernel.Domain` holds cross-context Value Objects with the same constraints as any context's Domain (zero NuGet refs beyond BCL, no context references).

**v1 contents:** `Money`, `Currency`.

**Promotion rule** (all four must hold): (1) Value Object, (2) identical semantics across all consumers, (3) crosses context boundaries as a typed object in real flows, (4) at least two contexts genuinely need it.

NOT eligible: Strongly-Typed IDs, `BillingPeriod`, `Address`, `Slug`, `TierLadder`, `TransitionPolicy`, `IntegratorKind`, `IntegratorTier`, `Aggregation` — single-context use today.

The kernel stays small by default. Adding a type requires explicit review; removing a type is the correct response to specialization pressure.

---

## 25. Published Language

Three published languages in Saasy:

1. **OpenAPI spec for the public REST API** — Saasy's contract with Integrators.
2. **Webhook payload schemas** — Saasy's contract with Integrator HTTP endpoints.
3. **Internal Domain Event Bus event schemas** — Saasy's contract **between bounded contexts** ([ADR-0016](../decisions/ADR-0016-event-driven-projections.md)).

Each context's outbound state-change events form the published language for downstream consumers' projections. Versioned per event type with the event name (e.g., `customer.created.v1`); breaking changes ship a new version (`customer.created.v2`); the producing context emits BOTH versions until consumers migrate, then v1 is retired.

---

## 26. Integration Events

Bus messages and Webhook payloads are integration events. Saasy's events serve **two purposes simultaneously** per [ADR-0016](../decisions/ADR-0016-event-driven-projections.md):

- **Reactive logic.** Cross-context Domain Services consume events to react (`WebhookTargetMatcher`, `RollupCatchupHandler` per [ADR-0012](../decisions/ADR-0012-threshold-firing-rules.md)).
- **Projection updates.** Each consuming context's projections are populated from these events.

To serve both, the catalog includes:

- **Reactive past-tense events:** `UsageThresholdCrossed`, `InvoiceGenerated`, `SubscriptionTransitioned`, `CustomerCreated`.
- **State-change events:** `IntegratorTimezoneChanged`, `CustomerBillingAddressChanged`, `PlanArchived`, `SubscriptionPaused`, etc.

Reactive events are a subset — every state change is also "something that just happened." Saasy treats them as one stream where the payload carries the full integration-shape snapshot at the new Version.

```csharp
// Domain Event (Saasy.Invoicing.Domain)
public sealed record InvoiceGenerated(InvoiceId InvoiceId, IntegratorId IntegratorId, DateTime OccurredOn);

// Integration shape (serialized into invoicing.outbox_messages.payload_json)
public sealed record InvoiceGeneratedIntegrationEvent(
    Guid EventId,
    string Event,            // "invoice.generated.v1"
    Guid InvoiceId,
    Guid IntegratorId,
    Guid SubscriptionId,
    string Currency,
    DateTime BillingPeriodStart,
    DateTime BillingPeriodEnd,
    DateTime IssuedAt,
    string Status,
    decimal Total,
    uint AggregateVersion,   // for projection idempotency
    DateTime OccurredAt);
```

---

## 27. Idempotent Handlers

Two flavors:

- **Event ingestion** — Saasy is idempotent on `idempotency_key`. Duplicate Events are silently dropped at the Metering ingestion path.
- **Webhook delivery** — Saasy guarantees at-least-once; Integrators MUST be idempotent on `event_id` of every payload. Documented in the API reference.

```csharp
public sealed class IngestEvent
{
    public sealed class Handler(IEventStore store)
    {
        public async Task<Result<Unit>> HandleAsync(IngestEventCommand command, CancellationToken ct)
        {
            if (await store.SeenIdempotencyKeyAsync(command.IdempotencyKey, ct))
                return Result<Unit>.Success(Unit.Value);
            await store.AppendAsync(command.ToEvent(), ct);
            return Result<Unit>.Success(Unit.Value);
        }
    }
}
```
