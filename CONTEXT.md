# Saasy — Domain Glossary

Strict, opinionated dictionary of Saasy's domain terms. Use these terms verbatim in issues, ADRs, PR text, code, comments, and conversation.

## Rules

- **Be opinionated.** When multiple words exist, pick the canonical one and list the rest under `_Avoid:_`.
- **Flag conflicts.** Genuinely ambiguous terms go under "Flagged ambiguities" with a resolution.
- **Tight definitions.** One sentence. Define what it IS, not what it does.
- **Show relationships and cardinality** where not obvious.
- **Only project-specific terms.** Skip general programming concepts.
- **Group by conceptual area.**

## Glossary

### Tenancy & Identity

- **Integrator** — a B2B SaaS company that integrates Saasy to meter usage and bill their customers; the top-level data isolation boundary. Each Integrator has a fixed **Integrator Kind**, an **Integrator Tier**, and a **Timezone**. _Avoid:_ tenant, partner, client, merchant, vendor.
- **Timezone** — IANA timezone name on an Integrator (`Europe/Oslo`, `America/Los_Angeles`); set at creation, required, forward-looking-mutable. Period Close resolves wall-clock midnight in this Timezone, then converts to UTC. _Avoid:_ tz, time zone (one word, IANA), tzid.
- **Integrator Kind** — Value Object on Integrator: `production` or `sandbox`, fixed at creation. Every aggregate scoped by IntegratorId is implicitly scoped by that Integrator's Kind; sandbox and production are completely separate Integrators with separate IDs, API keys, Customers, Plans. _Avoid:_ environment, mode, stage, realm.
- **Integrator Tier** — Value Object on Integrator capturing rate limits, ingestion ceilings, retention windows, feature flags; decoupled from Kind. _Avoid:_ plan (means something else in Saasy), package, sku.
- **Customer** — the **Integrator's** customer; the entity that holds a Subscription and gets Invoices. Fixed to one **Currency** at creation. Belongs to exactly one Integrator. _Avoid:_ end-customer, subscriber, account, end-user, client, tenant-customer.
- **Currency** — ISO 4217 code on a Customer; determines which Plans they can subscribe to and the Currency of every Line Item on their Invoices. Saasy never converts between Currencies. _Avoid:_ FX, denomination, money type.
- **User** — a person with login credentials acting on behalf of an Integrator or Customer; not the same as a Customer. _Avoid:_ member, person, principal.
- **Identity Mode** — how a Customer's Users authenticate: `IntegratorOwned` (Integrator-signed JWT embed) or `Federated` (Saasy is OIDC client of the Integrator's IdP). Per-Integrator. Saasy never hosts Customer credentials (per [ADR-0007](docs/decisions/ADR-0007-admin-dashboard-identity-and-customer-portal-posture.md)). _Avoid:_ auth strategy, login mode, sso mode, `saasy-idp` (removed).

### Product Modeling

- **Product Type** — Integrator-defined Aggregate Root declaring what they sell, owning the set of metered Dimensions and their Aggregations. _Avoid:_ product, sku, offering, item, catalog entry.
- **Dimension** — a single named, unit-bearing axis of metered usage on a Product Type (`api_calls`, `storage_gb`, `seats`); a Child Entity of Product Type. _Avoid:_ metric, meter, measure, axis, field.
- **Aggregation** — function applied to Events on a Dimension to produce a per-Billing-Period value: `sum`, `last`, `max`, `unique-count`, `time_weighted_last` (integral of value × duration across the period — Events define stepwise-constant intervals; the Rollup carries the time-weighted average). _Avoid:_ rollup-function, reducer, accumulator.
- **Plan** — Aggregate Root carrying mutable metadata (name, description, lifecycle status, **`IsExclusivePerCustomer`** flag) and referencing one Product Type; the lineage that groups one or more Plan Versions. When `IsExclusivePerCustomer = true`, a Customer may hold at most one active Subscription on any Plan Version of this Plan. _Avoid:_ tier, package, bundle, scheme, offering.
- **Plan Version** — immutable Aggregate Root referencing a Plan, fixing the Pricing Components, Cycle Anchor, Interval, and Currency prices for any Subscription that binds to it; never mutated, only superseded. _Avoid:_ plan revision, plan-v1, plan-state.
- **Subscription** — Aggregate Root binding one Customer to one Plan Version with a Billing Period schedule. A Customer may hold any number of active Subscriptions concurrently unless a Plan is exclusive (`Plan.IsExclusivePerCustomer = true`). A Subscription's Product Type is set by its initial Plan Version and never changes; Plan Transitions stay within the same Product Type. _Avoid:_ enrollment, contract, agreement, assignment.
- **Plan Transition** — operation of moving a Subscription from one Plan Version to another; governed by a Transition Policy. _Avoid:_ upgrade, downgrade, plan change, switch.
- **Transition Policy** — Plan-declared rules: `effect` (`immediate` | `next_period`) and `proration` (`daily` | `none`); overridable per Transition. _Avoid:_ change rule, switch policy, upgrade rule.
- **Proration** — day-by-day splitting of an in-flight Billing Period's charges between an old and new Plan Version on immediate Plan Transitions; Events attribute to whichever Plan Version was active at their timestamp. _Avoid:_ pro-rata, partial period charge, mid-cycle adjustment.

### Pricing Components

- **Pricing Component** — one priceable element of a Plan Version. _Avoid:_ pricing rule, charge type, fee.
- **Flat Fee** — fixed recurring charge per Billing Period. _Avoid:_ base fee, monthly charge, platform fee.
- **Per-Seat Fee** — recurring charge `unit_price × time_weighted_average_seat_count` over the Billing Period; reads from a Dimension whose Aggregation must be `time_weighted_last`. The Dimension code is configured per Pricing Component (`seats`, `users`, `licenses`). _Avoid:_ user fee, head-count charge, fixed seat fee.
- **Metered Charge** — usage-priced Pricing Component bound to one Dimension; carries a Quota and a pricing shape (flat-rate, tiered-graduated, tiered-volume). _Avoid:_ usage charge, variable fee.
- **Quota** — included usage allotment for a Dimension within one Billing Period before Overage applies; zero-quota is valid. _Avoid:_ entitlement, limit, allowance, cap.
- **Overage** — usage on a Dimension exceeding its Quota; priced by the Metered Charge's pricing shape. _Avoid:_ excess, overflow, surplus.
- **Tier** — band in a tiered Metered Charge with `from`, `to`, unit price; ladders come in `graduated` (each band priced separately) and `volume` (single band wins for total). _Avoid:_ bracket, slab, step, range.
- **Commitment** — Customer's pre-purchased usage or revenue commitment over a horizon, often discounted, with true-up at horizon end. _Avoid:_ contract, prepay, reserve.
- **Minimum** — per-Billing-Period floor charge; if computed charges fall below, the difference is added as a separate Line Item. _Avoid:_ floor, minimum-charge, MRC.
- **Credit** — prepaid balance drawn down by usage charges before any net amount appears on an Invoice. _Avoid:_ balance, prepay, voucher, gift card.

### Usage & Metering

- **Event** — single immutable usage record `{integrator_id, customer_id, dimension, value, timestamp, idempotency_key, properties{}}` ingested via the Event Hub or HTTP API. Events are NOT Aggregates — append-only stream records. _Avoid:_ usage record, measurement, sample, log entry, datapoint.
- **Ingestion** — accepting Events into Saasy via the **Event Hub** (primary) or **HTTP API** (alternative). _Avoid:_ intake, receive, post.
- **Event Hub** — Saasy's hosted streaming ingestion bus, one namespace per region shared across Integrator Kinds; per-Integrator scoped credentials, partitioned by Integrator. Implementation in [ADR-0001](docs/decisions/ADR-0001-ingestion-stream.md). _Avoid:_ service bus, message queue, kafka topic.
- **Rollup** — Aggregate Root holding the per-(Subscription, Dimension, Billing Period) aggregated total computed from Events using the Dimension's Aggregation; recomputed when Late Events arrive within the Late Event Window. _Avoid:_ aggregate, total, summary, window.
- **Billing Period** — recurring time window over which Rollups accumulate and at the end of which an Invoice is generated; configured per Subscription. _Avoid:_ cycle, period, billing window, term.
- **Cycle Anchor** — Plan-declared rule fixing when a Billing Period starts: `anchor-day` (period starts on the Subscription's anchor day each Interval, clamped to last day of month if that day-of-month doesn't exist) or `calendar-month` (period starts on the 1st). All resolution in the Integrator's Timezone. ([ADR-0010](docs/decisions/ADR-0010-period-boundaries-and-final-close.md)) _Avoid:_ billing day, cycle start, anchor date.
- **Interval** — length of one Billing Period: `monthly`, `quarterly`, or `annual`. Declared on the Plan Version. _Avoid:_ frequency, period length, term.
- **Period Close** — calendar moment a Billing Period ends: 00:00 wall-clock on the period's end day in the Integrator's Timezone, converted to UTC. Marks the Rollup as in its post-period state but does NOT issue the Invoice. _Avoid:_ cutoff, close, finalization, invoice trigger.
- **Final Close** — deterministic moment Period Close + Late Event Window has elapsed and the Invoice becomes computable; freezes the Rollup permanently. Late Events arriving after Final Close are rejected. _Avoid:_ true close, hard close, invoice cutoff.
- **Late Event** — Event whose `timestamp` falls inside an already-Period-Closed Billing Period; accepted only between Period Close and Final Close. _Avoid:_ backfill, retro event, out-of-order event.
- **Late Event Window** — grace period between Period Close and Final Close. Default 24 hours; per-Integrator override up to 7 days. _Avoid:_ grace, lookback, retro window.

### Enforcement & Alerting

- **Soft Enforcement** — Saasy's enforcement model: alert via Webhooks when usage crosses Thresholds; never block the Integrator's request path. _Avoid:_ async enforcement, observability mode, passive enforcement.
- **Threshold** — per-Subscription, per-Dimension percentage that triggers a `usage.threshold.crossed` Webhook. State-based firing rule with once-per-period record (see [ADR-0012](docs/decisions/ADR-0012-threshold-firing-rules.md)). _Avoid:_ alert level, trigger, watermark.
- **Threshold State** — per-(Subscription, Dimension, Billing Period) set of Threshold percentages that have already fired this period; lives on the Rollup, resets at Period Close. _Avoid:_ fired set, threshold log.
- **Webhook** — outbound HTTP notification fired by Saasy on a named event (`usage.threshold.crossed`, `invoice.generated`, `subscription.changed`, `customer.created`); signed and replayable; delivered at-least-once via the per-context Outbox plus the Internal Domain Event Bus. _Avoid:_ callback, hook, push notification.
- **Webhook Subscription** — Aggregate Root in the Delivery context holding the Integrator's Webhook URL, signing secret, and the set of Webhook kinds it wants delivered. _Avoid:_ webhook config, listener registration, endpoint.
- **Outbox Message** — per-context infrastructure record (NOT a domain Aggregate Root) carrying a serialized Domain Event in integration-event shape: `{id, integrator_id, event_kind, payload_json, occurred_at, relayed_at?}`. Written in the same DB transaction as the originating aggregate change; tailed by that context's Outbox Relay. _Avoid:_ pending event, queue entry, deferred message.
- **Outbox Relay** — per-context BackgroundService (one per emitting context, derived from a shared base) that tails its context's Outbox Message table and publishes to that context's bus topic, marking `relayed_at` on success. _Avoid:_ outbox worker, outbox publisher, dispatcher.
- **Internal Domain Event Bus** — Saasy's hosted Service Bus topology fanning out Domain Events from emitting contexts to channel-specific Dispatchers; one topic per emitting context (`saasy.domain-events.{context}`), session-keyed by `integrator_id`. Distinct from the Event Hub used for metering Ingestion. _Avoid:_ outbox bus, message bus, internal stream.
- **Outbox Dispatch** — per-(Outbox Message, channel, target) row recording one channel's delivery attempt and outcome. Enables fan-out to multiple channels with independent state per channel. _Avoid:_ delivery log, sent log, outbox attempt.
- **Dispatcher** — channel-specific Worker host (`Saasy.Workers.WebhookDispatcher`) that subscribes to the Internal Domain Event Bus, computes target fan-out, writes Outbox Dispatch rows, and performs outbound delivery. _Avoid:_ outbox dispatcher (the Outbox Relay is upstream of the bus; the Dispatcher is downstream), worker.

### Invoicing

- **Invoice** — Aggregate Root holding the structured, deterministic billing document produced at Final Close from a Subscription, its Plan Version, and its Rollups; v1 emits JSON and HTML, never PDF, never moves money. Owns Line Items as Child Entities. _Avoid:_ bill, statement, charge sheet.
- **Line Item** — Child Entity of Invoice; one row contributed by exactly one Pricing Component (or a Minimum top-up, or a Credit drawdown). _Avoid:_ row, entry, charge line.
- **Credit Note** — Aggregate Root representing an Integrator-issued correction that adjusts a previously generated Invoice; the only path to "fix" a closed Invoice. _Avoid:_ refund, reversal, adjustment, void.

### Surfaces

- **Admin Dashboard** — Integrator-facing web UI for configuring Product Types, Plans, Customers, Subscriptions, Webhooks, API keys. _Avoid:_ console, backoffice, control panel.
- **Customer Portal** — end-Customer-facing web UI showing their Subscription, current Rollups vs Quotas, past Invoices; embeddable iframe or white-labeled standalone. _Avoid:_ self-serve UI, client portal, account page.
- **Ops Console** — Saasy-internal web UI for provisioning Integrators, monitoring system health, metering Saasy's own usage. _Avoid:_ admin (overloaded), backoffice, internal-tool.

### Architecture & DDD Building Blocks

- **Aggregate Root** — top-level domain object, its own consistency boundary, the only access path to its Child Entities; carries a `uint Version` token; the only domain object that raises Domain Events; one Repository per Aggregate Root. _Avoid:_ aggregate, root entity, AR, top-level entity.
- **Child Entity** — entity owned by exactly one Aggregate Root, never accessed directly from outside, with no Repository of its own; factory is `internal`. _Avoid:_ sub-entity, owned entity, nested entity, dependent.
- **Value Object** — immutable type defined entirely by its attributes (no identity), implemented as a C# `record` for structural equality, self-validating via static `Create()`. _Avoid:_ VO, value type, struct, dto.
- **Strongly-Typed ID** — `readonly record struct` wrapping a `Guid` that gives each Aggregate Root its own ID type. _Avoid:_ typed id, wrapped guid, id wrapper.
- **Domain Event** — immutable record describing something significant that happened (`OrderPlaced`, `InvoiceGenerated`, `ThresholdCrossed`); raised inside Aggregate Root methods, written as an Outbox Message in the originating context's database in the same transaction as the aggregate change, then relayed to the Internal Domain Event Bus and consumed by Dispatchers AND Projection Updaters. Serves a dual role: reactive (downstream Domain Services react) and state-change (downstream Projections mirror upstream state). Each payload carries the full integration-shape snapshot of the aggregate at its new Version. _Avoid:_ event (overloaded — bare "Event" means the metering record), notification, integration message.
- **Projection** — per-consuming-context read model mirroring an upstream aggregate's state, populated by subscribing to that aggregate's state-change Domain Events on the Internal Domain Event Bus. NOT a domain aggregate. Used by Domain Services in the consuming context for cross-context reads instead of foreign Repository access. Eventually consistent; surfaces 425 + Retry-After at the API edge when a referenced upstream entity is not yet propagated. _Avoid:_ replica, cache, copy, view.
- **Projection Updater** — per-(consuming context, upstream topic) BackgroundService that consumes state-change Domain Events from the bus and applies them to projection tables. Idempotent on retry via the aggregate's `Version` field; out-of-order delivery is prevented by Service Bus session keying on `integrator_id`. _Avoid:_ projection worker, sync worker, replicator.
- **Domain Service** — stateless service holding business logic that spans multiple Aggregate Roots or requires Repository access; validates and constructs but never persists. _Avoid:_ service, manager, helper, coordinator.
- **Repository** — port (interface) defined in the Domain layer for one Aggregate Root, implemented as an EF Core adapter in the Infrastructure layer; one per Aggregate Root, never per Child Entity. _Avoid:_ store, dao, data access.
- **Unit of Work** — Application-layer port abstracting the transactional boundary; its EF Core implementation calls `SaveChangesAsync` and translates `DbUpdateConcurrencyException` to `ConcurrencyException` and `DbUpdateException` to `PersistenceException`. _Avoid:_ transaction, scope, uow.

### Application Layer

- **Vertical Slice** — one C# file per use case: a `public static class {UseCase}` containing a nested `Command` or `Query` record and a nested `Handler`. _Avoid:_ feature folder, slice, use case file.
- **Command** — immutable record carrying input for a state-changing use case, named `{UseCase}Command`; typically has a co-located `Validate()` method. _Avoid:_ request, dto, message.
- **Query** — immutable record carrying input for a read-only use case, named `{UseCase}Query`; never has a `Validate()` method. _Avoid:_ request, lookup, search.
- **Handler** — non-static class nested inside a Vertical Slice, with a single `HandleAsync` returning `Result<T>` (commands) or DTO/`null` (queries); DI-registered as the concrete `{UseCase}.Handler` type. _Avoid:_ command handler, executor, processor (informal only).
- **Result** — discriminated outcome type returned by Command Handlers: `Success`, `Failure`, `NotFound`, `Conflict`, `ValidationFailure`, `NotYetPropagated`; the API layer maps each variant to an HTTP status code. _Avoid:_ outcome, response wrapper, either.

### Layers & Hosts

- **Domain layer** — `Saasy.{Bounded Context}.Domain` projects holding Aggregate Roots, Child Entities, Value Objects, Domain Events, Domain Services, Repository interfaces; zero NuGet references beyond the BCL. _Avoid:_ core, model.
- **Application layer** — `Saasy.{Bounded Context}.Application` projects holding Vertical Slices, Commands/Queries, Handlers, Result, `IUnitOfWork`, application-orchestration ports; depends only on its Domain. _Avoid:_ services, use cases, business.
- **Infrastructure layer** — `Saasy.{Bounded Context}.Infrastructure` projects holding EF Core `DbContext`s, Repository adapters, `UnitOfWork`, `EntityTypeConfiguration`s, the Domain-Event-to-Outbox interceptor, and adapters for external services. _Avoid:_ data, persistence (broader than just persistence).
- **Api host** — `Saasy.Api`; thin adapter mapping HTTP requests to Commands/Queries and Result variants to status codes; composition root for the API process. _Avoid:_ web, server.
- **Worker host** — a `Saasy.Workers.{Name}` project (`Saasy.Workers.RollupWorker`, `Saasy.Workers.InvoiceGenerator`, `Saasy.Workers.WebhookDispatcher`); Aspire-orchestrated service depending on Application + Infrastructure. _Avoid:_ worker service, background job, daemon.
- **Aspire AppHost** — `Saasy.AppHost`; orchestrates Api host, Worker hosts, Postgres, Event Hubs, Service Bus, and seed configuration in dev; produces deployment artifacts for production. Composition root for the distributed system. _Avoid:_ host, orchestrator (informal only).

## Relationships

- An **Integrator** owns many **Customers**, many **Product Types**, and many **Plans**. Its **Integrator Kind** is fixed at creation; production and sandbox traffic are partitioned by being separate Integrators, not by an Environment property on aggregates.
- A **Plan** belongs to one **Product Type** and is referenced by many **Plan Versions** (separate aggregates); editing a Plan creates a new Plan Version with a fresh ID, never mutates an existing Plan Version.
- A **Customer** may hold zero or more active **Subscriptions**, any number on any Plans, unless `Plan.IsExclusivePerCustomer = true`. A Subscription points to exactly one **Plan Version** and stays within one **Product Type** for its lifetime.
- A **Plan Version** is composed of many **Pricing Components**; a **Metered Charge** references exactly one **Dimension** of the Plan's Product Type.
- An **Event** is bound to one Customer and one Dimension; many Events flow into one **Rollup** keyed by (Subscription, Dimension, Billing Period).
- At **Final Close**, Saasy computes one **Invoice** per Subscription from its Plan Version's Pricing Components and the period's Rollups; each Pricing Component contributes zero or more **Line Items**.
- A **Threshold** crossing on a Rollup raises a Domain Event that is written as an **Outbox Message** in the Metering context, relayed to the Internal Domain Event Bus, consumed by the Webhook Dispatcher, fanned out to every matching **Webhook Subscription** via an **Outbox Dispatch** row, and delivered as a **Webhook**.
- **Identity Mode** is per-Integrator; **Users** authenticate accordingly regardless of which surface they enter through.

## Layering convention

Saasy follows a four-layer onion plus an Aspire-orchestrated host topology:

- **Domain → Application → Infrastructure → (Api host | Worker host)**, dependencies pointing inward.
- The Domain layer has zero external dependencies and is testable with plain object construction.
- Repository interfaces and Domain Services live in **Domain**; their EF Core implementations live in **Infrastructure**.
- `IUnitOfWork`, application-orchestration ports, Vertical Slices, Commands/Queries, Handlers, and `Result` live in **Application**.
- The **Api host** and each **Worker host** are thin composition roots calling `AddApplication()` and `AddInfrastructure()`.
- The **Aspire AppHost** orchestrates dev and shapes deployment for production.
- Bounded contexts (`Tenancy`, `Catalog`, `Subscriptions`, `Metering`, `Invoicing`, `Delivery`) each have their own Domain / Application / Infrastructure trio. Cross-context references use IDs only.

The architectural pattern is documented in [docs/architecture/architecture.md](docs/architecture/architecture.md). The aggregate map lives in [docs/architecture/aggregates.md](docs/architecture/aggregates.md). The pattern catalog is [docs/architecture/patterns.md](docs/architecture/patterns.md).

## Flagged ambiguities

- **"Customer"** — always means the **Integrator's** customer, never Saasy's customer. Saasy's customer is the **Integrator**. Never write "Saasy customer" — write "Integrator".
- **"Tenant"** — banned. The data-isolation unit is the **Integrator**. The word creates confusion with multi-tenant patterns and with the Integrator's own customers; avoid even informally.
- **"Plan" vs "Plan Version" vs "Subscription"** — **Plan** is the lineage (mutable metadata). **Plan Version** is the immutable contract referenced by Subscriptions. **Subscription** is one Customer's binding to one Plan Version. Plan and Plan Version are SEPARATE Aggregate Roots — never store Plan Versions as a child collection of Plan. Never write "the customer's plan" — write "the customer's Subscription".
- **"Quota" vs "Entitlement"** — Saasy uses **Quota** for numeric, periodic, dimension-scoped allotments. "Entitlement" is reserved (not yet used) for future boolean feature-access flags.
- **"Tier"** — bare **Tier** is a band in a tiered Metered Charge ladder. **Integrator Tier** (always with the qualifier) is the Value Object on Integrator. Never use bare "tier" to mean a Plan ("starter tier", "pro tier") — those are Plans. Never use bare "tier" to mean an Integrator's plan with Saasy — that's an Integrator Tier.
- **"Environment"** — banned as a domain term on aggregates. Sandbox and production are separate Integrators distinguished by **Integrator Kind** ([ADR-0009](docs/decisions/ADR-0009-integrator-per-kind-environment-model.md)). The word remains acceptable in informal prose meaning the runtime context an Integrator operates in, but is never a property on an aggregate, a column on a table, or a scoping predicate on a query.
- **"Event"** vs **"Domain Event"** — bare **Event** is the metering ingest record. **Domain Event** is the DDD concept (raised by an Aggregate Root, persisted to the Outbox). DIFFERENT concepts.
- **"Webhook"** vs **"Outbox Message"** vs **"Outbox Dispatch"** — **Outbox Message** is the per-context, channel-agnostic record of "this Domain Event happened." **Outbox Dispatch** is one channel's per-target attempt log derived from an Outbox Message. **Webhook** is the outbound HTTP POST that fulfils a Webhook-channel Outbox Dispatch row. Don't conflate them.
- **"Enforcement"** — in Saasy this means **Soft Enforcement** only: alerting via Webhooks. Saasy never blocks requests in the Integrator's hot path.
- **"Repository"** — only exists for Aggregate Roots, never for Child Entities or Value Objects. Never `IDimensionRepository` or `ILineItemRepository`.
- **"Service"** — banned as a bare term. Always say **Domain Service** or **Handler** or refer to the specific role. Never an `IFooService` interface unless it's an external-system port (then prefer concrete role names like `IPaymentGateway`).
- **"Saasy"** — always the project. The product is also Saasy. There is no "engine" / "platform" disambiguation yet.
