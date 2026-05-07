# Saasy Aggregate Map

Map of Saasy's Aggregate Roots, Child Entities, and Value Objects per bounded context, with boundary reasoning, key invariants, and cross-aggregate references. Vocabulary per [CONTEXT.md](../../CONTEXT.md). Boundary patterns in [architecture.md](architecture.md#aggregate-roots).

**Projections.** Each consuming context maintains projection tables of upstream data, kept in sync via state-change events on the Internal Domain Event Bus per [ADR-0016](../decisions/ADR-0016-event-driven-projections.md). Projections are NOT aggregates — they have no domain invariants beyond "mirror the upstream's published state." They are infrastructure read models. The "Projections held" subsections list which upstream aggregates each context mirrors locally.

---

## Tenancy

| Type | Name | Parent | Key Properties |
|---|---|---|---|
| **Aggregate Root** | Integrator | — | IntegratorId, Name, Slug, Kind, Tier, Timezone, IdentityMode, Settings |
| **Child Entity** | ApiKey | Integrator | ApiKeyId, HashedSecret, CreatedAt, RevokedAt |
| **Aggregate Root** | Customer | — | CustomerId, IntegratorId, ExternalId, BillingAddress, Currency, CreatedAt |
| **Value Object** | Address | — | Street, City, PostalCode, Country |
| **Value Object** | Slug | — | normalized lowercase identifier |
| **Value Object** | IntegratorKind | — | `production` or `sandbox`; fixed at Integrator creation |
| **Value Object** | IntegratorTier | — | rate-limit, ingestion-ceiling, retention-window, feature-flag bundle; mutable; decoupled from Kind |
| **Value Object** | Timezone | — | IANA timezone name; required at Integrator creation, forward-looking-mutable |

### Boundary reasoning

- **Integrator** is the top-level data-isolation boundary, referenced by every other aggregate. Its `Kind` distinguishes production from sandbox traffic ([ADR-0009](../decisions/ADR-0009-integrator-per-kind-environment-model.md)).
- **ApiKey** is a Child Entity — meaningful only within an Integrator, bounded count (<100), no external "look up by ID" use case. ApiKeys do NOT carry an Environment field; the Integrator's Kind differentiates production vs sandbox keys.
- **Customer** is a separate Aggregate Root (not a Child of Integrator) — an Integrator could have hundreds of thousands; loading the Integrator must not load all Customers.

### Key invariants

- An Integrator's Slug is unique across all Integrators.
- An Integrator's Kind is fixed at creation; promoting sandbox to production is not supported (the customer creates a new production Integrator).
- An Integrator's Timezone is required at creation and forward-looking-mutable: already-issued Invoices and already-Final-Closed Periods stay in the Timezone they were resolved with; new Periods use the new Timezone. Mid-period changes do not retroactively shift in-flight boundaries. ([ADR-0010](../decisions/ADR-0010-period-boundaries-and-final-close.md))
- A Customer's Currency is fixed at creation.
- A Customer belongs to exactly one Integrator (and inherits its Kind).

### Cross-aggregate references

- `Customer.IntegratorId → Integrator`
- Every other aggregate carries `IntegratorId` for query scoping.

### Projections held

None. Tenancy is the most upstream context.

---

## Catalog

| Type | Name | Parent | Key Properties |
|---|---|---|---|
| **Aggregate Root** | ProductType | — | ProductTypeId, IntegratorId, Name, Description, Status |
| **Child Entity** | Dimension | ProductType | DimensionId, Code, Unit, Aggregation (`sum` \| `last` \| `max` \| `unique-count` \| `time_weighted_last`) |
| **Aggregate Root** | Plan | — | PlanId, ProductTypeId, IntegratorId, Name, Description, LifecycleStatus, IsExclusivePerCustomer |
| **Aggregate Root** | PlanVersion | — | PlanVersionId, PlanId, Interval, CycleAnchor, Components, CurrencyPrices, CreatedAt |
| **Child Entity** | PricingComponent | PlanVersion | ComponentId, Kind (Flat \| PerSeat \| Metered), Configuration |
| **Value Object** (`Saasy.SharedKernel.Domain`) | Money | — | Amount, Currency |
| **Value Object** (`Saasy.SharedKernel.Domain`) | Currency | — | ISO 4217 code |
| **Value Object** | TierLadder | — | Tiers (ordered list), Mode (Graduated \| Volume) |

`PerSeat` carries `unit_price` + `dimension_code` referencing a `time_weighted_last` Dimension on the Plan's Product Type. `Metered` carries `dimension_code` + a pricing shape (flat-rate, tiered-graduated, or tiered-volume).

### Boundary reasoning

- **ProductType** is an Aggregate Root, owning Dimensions; an Integrator's catalog is configured at this level.
- **Dimension** is a Child Entity — bounded count (<50), and ProductType enforces unique codes and locks Aggregation choice once Events have been ingested.
- **PlanVersion is a SEPARATE Aggregate Root from Plan** — the most consequential decision in this context. Subscriptions reference PlanVersion directly; PlanVersions are immutable; editing a Plan creates a new PlanVersion. Putting PlanVersions inside Plan would force loading every historical version every time a Plan is touched.
- **PricingComponent** is a Child Entity — only meaningful within a PlanVersion; the PlanVersion enforces invariants across components (Tier ladders valid, Currency coverage consistent).
- **Money and Currency** live in `Saasy.SharedKernel.Domain` per [ADR-0013](../decisions/ADR-0013-shared-kernel.md). Catalog imports them like every other context.

### Key invariants

- A PlanVersion is immutable once created — no `Update*` methods on the type.
- Editing a Plan produces a new PlanVersion with a fresh PlanVersionId on the same PlanId.
- A PlanVersion's currency price set must include every Currency claimed in its Pricing Components.
- Tier ladders are non-overlapping and ordered ascending by `from`.
- A Subscription can only bind to a PlanVersion whose Currency set includes the Customer's Currency.
- A Per-Seat Fee Pricing Component's `dimension_code` must reference a Dimension whose Aggregation is `time_weighted_last`. ([ADR-0011](../decisions/ADR-0011-per-seat-fee-time-weighted-aggregation.md))
- `Plan.IsExclusivePerCustomer` is mutable but does not retroactively cancel existing Subscriptions if flipped from `false` to `true`. ([ADR-0015](../decisions/ADR-0015-customer-subscription-cardinality.md))

### Cross-aggregate references

- `Plan.ProductTypeId → ProductType`
- `PlanVersion.PlanId → Plan`
- (`Subscription.PlanVersionId → PlanVersion`, defined in Subscriptions)

### Cross-aggregate coordination

`PlanVersioningService` validates new Pricing Components against the Plan's ProductType Dimensions and constructs the immutable PlanVersion.

### Projections held

- **Integrator projection** — IntegratorId, Kind, Tier, Timezone (for surfacing tier-limit feedback when Integrators try to add Plans/Product Types beyond their tier).

---

## Subscriptions

| Type | Name | Parent | Key Properties |
|---|---|---|---|
| **Aggregate Root** | Subscription | — | SubscriptionId, CustomerId, PlanVersionId, IntegratorId, AnchorDate, CurrentPeriodStart, CurrentPeriodEnd, Status, Thresholds |
| **Child Entity** | ThresholdConfig | Subscription | ThresholdConfigId, DimensionCode, Percentages |
| **Value Object** | TransitionPolicy | — | Effect (Immediate \| NextPeriod), Proration (Daily \| None) |
| **Value Object** | BillingPeriod | — | Start, End |

### Boundary reasoning

- **Subscription** is an Aggregate Root with its own state machine and high-frequency mutations (Threshold updates, Period rollovers, Plan Transitions). Referenced by Invoice and Rollup via ID.
- **ThresholdConfig** is a Child Entity — bounded count (one per metered Dimension); Subscription enforces "no duplicate Dimension thresholds" and "percentages in [0, 1.5]".
- **Plan Transitions do NOT have their own aggregate** — a Transition is `Subscription.Transition(targetPlanVersionId, effect, proration, time)`, recorded as state changes + Domain Events.

### Key invariants

- A Subscription is bound to exactly one PlanVersion at a time.
- A Customer may hold any number of active Subscriptions concurrently; the optional limit is `Plan.IsExclusivePerCustomer = true`, enforced by `SubscriptionLifecycleService.CreateAsync` ([ADR-0015](../decisions/ADR-0015-customer-subscription-cardinality.md)). DB-level enforcement is deferred.
- A Subscription's Product Type is set at creation (from its initial PlanVersion) and never changes. Plan Transitions stay within the same Product Type.
- A Plan Transition with `effect: immediate` triggers Proration; with `next_period`, the change queues.
- A Subscription cannot transition to a PlanVersion that doesn't price the Customer's Currency.
- Status transitions: `Draft → Active → Paused/Cancelled` (terminal).
- For `anchor-day` Cycle Anchor, the Subscription stores its original anchor day (1–31) at creation; period boundary resolution clamps to the last day of the target month when the anchor day exceeds it. Annual anchors on Feb 29 clamp to Feb 28 in non-leap years. ([ADR-0010](../decisions/ADR-0010-period-boundaries-and-final-close.md))
- `Subscription.AddThreshold(...)` raises `ThresholdAdded`; Metering performs catch-up firing. `Subscription.RemoveThreshold(...)` raises `ThresholdRemoved` and stops future firing but does NOT clear prior `ThresholdState`. ([ADR-0012](../decisions/ADR-0012-threshold-firing-rules.md))

### Cross-aggregate references

- `Subscription.CustomerId → Customer` (Tenancy)
- `Subscription.PlanVersionId → PlanVersion` (Catalog)
- `Subscription.IntegratorId → Integrator` (Tenancy)

### Cross-aggregate coordination

`SubscriptionLifecycleService` validates Customer existence + Currency match against PlanVersion before subscribe / transition. It consumes **projection-backed lookup ports** (`ICustomerLookup`, `IPlanLookup`, `IPlanVersionLookup`) defined in `Saasy.Subscriptions.Domain` per [ADR-0016](../decisions/ADR-0016-event-driven-projections.md). If a referenced upstream entity is not yet in the local projection, the service returns `Result.NotYetPropagated(...)` → HTTP 425 + `Retry-After`.

### Projections held

- **Customer projection** — CustomerId, IntegratorId, Currency.
- **Plan projection** — PlanId, ProductTypeId, IntegratorId, IsExclusivePerCustomer.
- **PlanVersion projection** — PlanVersionId, PlanId, supported Currencies, Pricing Component summaries.

---

## Metering

| Type | Name | Parent | Key Properties |
|---|---|---|---|
| **NOT an aggregate** | Event | — (append-only stream) | IntegratorId, CustomerId, Dimension, Value, Timestamp, IdempotencyKey, Properties |
| **Aggregate Root** | Rollup | — | RollupId, SubscriptionId, DimensionCode, BillingPeriod, AggregatedValue, ThresholdState; for `time_weighted_last`, also IntegralAccumulator and LastValueTimestamp |

### Boundary reasoning

- **Event** is NOT an aggregate — append-only stream record, no mutating state. Persisted to Event Hubs (with Capture archive) and to a Postgres `events_recent` table for the Late Event Window.
- **Rollup** is an Aggregate Root — mutable AggregatedValue as Events flow in; ThresholdState lives here so each Threshold fires exactly once per period; high-frequency contention target keyed on Subscription.
- **Why keyed by Subscription, not Customer.** A Customer's Subscription anchors the Billing Period; if the Customer transitions Plans mid-period, the new Subscription period starts a new Rollup. Keying on Subscription matches the Plan Version that owns the period's pricing.

### Key invariants

- A Rollup is uniquely identified by `(SubscriptionId, DimensionCode, BillingPeriodStart)`.
- AggregatedValue is recomputed from Events using the Dimension's Aggregation.
- For `time_weighted_last`: Events define stepwise-constant intervals; AggregatedValue = `(∫ value(t) dt over [PeriodStart, PeriodEnd]) / period length`. Late Events arriving in the Window cause full re-integration; only `(timestamp, value)` pairs affect the result, not arrival order. Per-Seat Fees consume this directly. ([ADR-0011](../decisions/ADR-0011-per-seat-fee-time-weighted-aggregation.md))
- ThresholdState resets at Period Close; never carries across periods.
- Threshold firing rule: after every Rollup recompute, for each percentage `p` in the active config where `AggregatedValue / Quota >= p` AND `p ∉ ThresholdState`: fire `usage.threshold.crossed`, add `p` to ThresholdState. State-based, once-per-period. ([ADR-0012](../decisions/ADR-0012-threshold-firing-rules.md))
- Late Events in the Window cause Rollup recomputation; outside the Window (after Final Close), they are rejected. ([ADR-0010](../decisions/ADR-0010-period-boundaries-and-final-close.md))
- A duplicate `IdempotencyKey` is silently dropped.

### Cross-aggregate references

- `Rollup.SubscriptionId → Subscription`
- `Event.IntegratorId / CustomerId → respective aggregates` (validated at ingestion)

### Cross-aggregate coordination

`RollupService` applies Aggregations and emits `UsageThresholdCrossed` Domain Events. Subscription / ProductType data comes from local projections.

### Projections held

- **Subscription projection** — SubscriptionId, CustomerId, IntegratorId, ProductTypeId, current Billing Period, ThresholdConfig, Status.
- **ProductType + Dimension projection** — ProductTypeId, Dimensions list with Code / Unit / Aggregation.

---

## Invoicing

| Type | Name | Parent | Key Properties |
|---|---|---|---|
| **Aggregate Root** | Invoice | — | InvoiceId, SubscriptionId, IntegratorId, BillingPeriod, Currency, IssuedAt, Status, Lines |
| **Child Entity** | LineItem | Invoice | LineItemId, Source (PricingComponent / Minimum / CreditDrawdown), Description, Quantity, UnitAmount, TotalAmount |
| **Aggregate Root** | CreditNote | — | CreditNoteId, InvoiceId, IntegratorId, IssuedAt, Reason, Adjustments |
| **Child Entity** | CreditNoteAdjustment | CreditNote | AdjustmentId, OriginalLineItemId, AdjustedAmount |

### Boundary reasoning

- **Invoice** is an Aggregate Root with its own lifecycle (Draft → Issued → terminal). Once Issued, immutable.
- **LineItem** is a Child Entity — bounded count; Invoice enforces currency consistency, totals reconciliation, and snapshotted PricingComponent values.
- **CreditNote** is a separate Aggregate Root — its own audit trail; modifying it must not require loading the original Invoice (which is immutable).
- **CreditNoteAdjustment** is a Child Entity — bounded count.

### Key invariants

- An Issued Invoice is immutable.
- All Line Items on an Invoice share the same Currency (the Customer's Currency at Period Close).
- Line Items snapshot PricingComponent configuration at the moment of Invoice generation.
- A CreditNote can only reference an Invoice that exists and was Issued.
- Sum of CreditNote adjustments cannot exceed the original Invoice's totals.

### Cross-aggregate references

- `Invoice.SubscriptionId → Subscription`
- `Invoice.IntegratorId → Integrator`
- `CreditNote.InvoiceId → Invoice`
- `CreditNoteAdjustment.OriginalLineItemId → LineItem` (Child Entity reference allowed because Credit Notes need to identify which Line Item they're adjusting; recorded by ID, no navigation)

### Cross-aggregate coordination

`InvoiceComputationService` walks a Subscription's PlanVersion Pricing Components, evaluates each against the period's Rollups, and produces deterministic Line Items. Subscription, PlanVersion (with full Pricing Component shape), Rollup, Customer, and Integrator data come from local projections. The Handler persists the Invoice; `Invoice.Issue()` raises `InvoiceGenerated`.

### Projections held

Invoicing has the deepest projection footprint because it needs the complete shape of every upstream aggregate involved in Invoice generation:

- **Subscription projection** — full Subscription shape including ThresholdConfig and Status.
- **PlanVersion projection (full)** — including Pricing Components with all configuration (Quotas, Tier ladders, unit prices, Per-Seat Fee `dimension_code`s).
- **Rollup projection** — RollupId, SubscriptionId, DimensionCode, BillingPeriod, AggregatedValue. Read at Final Close once and snapshotted onto the Invoice.
- **Customer projection** — CustomerId, BillingAddress (snapshotted onto the Invoice at issuance), Currency.
- **Integrator projection** — IntegratorId, Timezone, Name (for Invoice header rendering).

---

## Delivery

The Delivery context owns Saasy's outbound integration concerns: Integrator-controlled targets that should receive Domain Events, plus the Dispatcher workers that perform delivery. v1 ships one channel (Webhook); the context admits sibling target aggregates for future channels (external Service Bus, external Event Hub) without restructuring.

| Type | Name | Parent | Key Properties |
|---|---|---|---|
| **Aggregate Root** | WebhookSubscription | — | WebhookSubscriptionId, IntegratorId, Url, SigningSecret, SubscribedKinds, Status |

### Boundary reasoning

- **WebhookSubscription** is an Aggregate Root — an Integrator may have many (different URLs for different concerns); each has its own lifecycle (active, paused, secret rotation).
- **OutboxMessage is NOT an Aggregate Root — it is per-context infrastructure.** An emitting context's `DomainEventToOutboxInterceptor` writes only into its own context's `outbox_messages` table; the table carries no domain invariants beyond "row written atomically with the aggregate change" and has no Repository.
- **OutboxDispatch is NOT an Aggregate Root either.** Operational state (attempt count, last error, processed-at) owned by the Dispatcher worker.
- **Future channels add sibling aggregates.** External Service Bus / Event Hub delivery to Integrator-owned namespaces (post-v1) become their own Aggregate Roots (`ServiceBusEndpoint`, `EventHubEndpoint`) with corresponding Dispatcher workers.

### Key invariants

- A WebhookSubscription's URL is HTTPS only.
- SubscribedKinds is a non-empty subset of supported Webhook kinds.

### Cross-aggregate references

- `WebhookSubscription.IntegratorId → Integrator`

### Cross-aggregate coordination

Target fan-out is performed by the **Webhook Dispatcher worker** when it consumes a bus message, NOT by an interceptor in the originating context's `SaveChangesAsync`. This avoids any context's interceptor needing to read Delivery-context tables. The Dispatcher writes one OutboxDispatch row per matched target as it claims the bus message, then delivers each row independently with retry-with-backoff.

Detailed flow in [architecture.md](architecture.md#domain-events-and-the-outbox) and [patterns.md §21](patterns.md#21-transactional-outbox-per-context--internal-domain-event-bus).

### Projections held

- **Integrator projection** — IntegratorId, Kind, Tier (for Tier-based dispatch policy if introduced; cheap to maintain).

WebhookSubscription itself is owned by Delivery (source of truth) and does not require a projection.

---

## Cross-Context Reference Map

```
Tenancy
  Integrator ◄─────────────── Customer
                       │
                       │ (referenced by ID from every other context)
                       ▼
Catalog
  ProductType ◄──── Plan ◄──── PlanVersion
                                    ▲
Subscriptions                       │
  Subscription ─────────────────────┘
       ▲
Metering
  Rollup ───► (raises Domain Events when Threshold crossed)
       │
       │  metering.outbox_messages ──► Metering Outbox Relay
       │
Invoicing
  Invoice ──► (raises Domain Events when Issued)
       │
       │  invoicing.outbox_messages ──► Invoicing Outbox Relay
       ▼
Internal Domain Event Bus (Service Bus topics, one per emitting context,
                            session-keyed by integrator_id)
       ▼
Delivery
  Webhook Dispatcher ──► outbox_dispatch rows per matched target
                              │
                              ▼
                     WebhookSubscription (target lookup)
                              │
                              ▼
                     HTTPS POST to Integrator's URL
```

All cross-aggregate references are by Strongly-Typed ID. No navigation properties or FK constraints cross aggregate boundaries. Existence is validated in Domain Services via `ExistsAsync` Repository methods (within a context) or projection-backed lookup ports (across contexts).

Outbox tables (`{context}.outbox_messages`) and dispatch tables (`{channel}.outbox_dispatch`) are infrastructure, not aggregates — they appear in the diagram for flow context but have no Repositories or Domain Services.
