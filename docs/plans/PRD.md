---
id: PRD-001
type: prd
state: draft
title: Saasy — usage metering, entitlements, and invoice generation for B2B SaaS
created: 2026-05-03
updated: 2026-05-07
authors: [kim]
---

# Saasy — Product Requirements

## 1. Vision

Saasy is a backend service that B2B SaaS products integrate with to offload usage metering, plan/entitlement modeling, and Invoice generation. Integrators define their Product Types and Plans in Saasy, push Events as they happen (via the hosted Event Hub or HTTP API), and Saasy computes per-Customer usage, fires Webhook alerts on Quota Threshold crossings, and produces ready-to-charge Invoices on each Period Close. Payments and Invoice delivery remain the Integrator's responsibility in v1.

The wedge: metering + plan-modeling + invoice-generation is hard, repetitive, and undifferentiated for every B2B SaaS that bills on usage. Closest analogues: Lago, Orb, Metronome. Differentiator: ergonomic configurable Product Types and a strong embeddable Customer Portal out of the box.

## 2. Personas

| Persona | Who | Primary jobs |
|---|---|---|
| **Integrator product/eng** | Engineer at a SaaS company integrating Saasy | Configure Product Types, Plans, Dimensions; wire Event ingestion; query usage |
| **Integrator finance/ops** | RevOps / finance | Review Invoices, manage Plan Transitions, audit usage, handle disputes |
| **Customer admin** | Admin User at the Integrator's Customer organization | View own usage, see Subscription, view past Invoices, request Plan Transitions |
| **Saasy ops** | Saasy's own team | Provision Integrators, monitor health, support, meter Saasy's own usage |

## 3. Decisions

| Axis | Decision |
|---|---|
| Product kind | Metering + entitlements + Invoice generation platform that Integrators embed |
| Enforcement | **Soft Enforcement** — Saasy is NOT in the Integrator's hot path; alerts fire via Webhook |
| Billing scope | Metering + Invoice generation; **no payments in v1** |
| UI surfaces | Admin Dashboard + Customer Portal (embeddable + white-label) + Ops Console + API/Webhooks |
| Pricing | Flat Fee, Per-Seat Fee, Metered Charges (flat-rate, Quota+Overage, tiered graduated/volume), Commitments, Minimums, Credits — phased; see §7 |
| Identity Modes | Two per Integrator: `IntegratorOwned` (first), `Federated`. Saasy never hosts Customer credentials ([ADR-0007](../decisions/ADR-0007-admin-dashboard-identity-and-customer-portal-posture.md)) |
| Cycle Anchor | Per-Plan: `anchor-day` or `calendar-month`, monthly / quarterly / annual Interval |
| Plan Transition Policy | Per-Plan default; overridable per Transition |
| Plan versioning | Sticky — existing Subscriptions never auto-migrate; migration is an explicit Transition |
| Currency | Customer fixed to one Currency; Plans declare prices per Currency; no FX |
| Webhooks | One per Threshold crossing (no digest mode in v1) |
| Sandbox vs Production | **Integrator Kind** (`production` or `sandbox`) on the Integrator, fixed at creation; sandbox and production are separate Integrators with no shared data ([ADR-0009](../decisions/ADR-0009-integrator-per-kind-environment-model.md)). Full feature parity; default sandbox **Integrator Tier** runs under tighter limits |

## 4. Functional scope

### 4.1 Core domain (v1)

- **Integrators.** Top-level data-isolation boundary. Each has a fixed **Integrator Kind** and a configurable **Integrator Tier**. Sandbox and production are separate Integrators (no shared data, no cross-environment leakage to enforce). Sandbox is feature-identical but defaults to a free, capped Tier; the Admin Dashboard shows a sandbox badge. ([ADR-0009](../decisions/ADR-0009-integrator-per-kind-environment-model.md))
- **Customers.** Belong to one Integrator. Hold external-id, billing address, **Currency** (fixed at creation), and Subscriptions. Identity authentication is pluggable per Integrator deployment.
- **Product Types.** Configurable schema declaring what the Integrator sells: name, description, set of metered **Dimensions** (name, unit, **Aggregation**: `sum` | `last` | `max` | `unique-count` | `time_weighted_last`). The `time_weighted_last` Aggregation is required for any Dimension consumed by a Per-Seat Fee.
- **Plans.** Reference one Product Type; each edit produces a new immutable **Plan Version**. Existing Subscriptions stay bound to their original Plan Version. Each Plan declares its **Cycle Anchor** and **Interval**. All Period boundaries resolve at 00:00 wall-clock in the **Integrator's Timezone** and convert to UTC ([ADR-0010](../decisions/ADR-0010-period-boundaries-and-final-close.md)). Each Plan declares prices in one or more Currencies; a Customer can subscribe only if the Plan has prices in their Currency. **Pricing Components**:
  - **Flat Fee** (recurring, per Interval)
  - **Per-Seat Fee** (`unit_price × time_weighted_average_seat_count`; references a `time_weighted_last` Dimension; Integrator picks the code, e.g., `seats`, `users`, `licenses`)
  - **Metered Charge** per Dimension: flat-rate, Quota + Overage, tiered (graduated and volume) — _phased, see §7_
  - **Commitments**, **Minimums**, **Credits** — _phase 5_
- **Subscriptions.** A Customer may hold any number of active Subscriptions concurrently unless `Plan.IsExclusivePerCustomer = true` ([ADR-0015](../decisions/ADR-0015-customer-subscription-cardinality.md)). Each binds to exactly one Plan Version. **Plan Transitions** stay within the same Product Type. Each Plan declares a default **Transition Policy**: `effect` (`immediate` — Proration computed; `next_period` — change queued) and `proration` (`daily` — split day-by-day, attributing Events by timestamp; `none` — old Plan Version owns the period). Integrators may override per Transition.
- **Events.** Append-only stream: `{integrator_id, customer_id, dimension, value, timestamp, idempotency_key, properties{}}`. Late Events accepted within the configured Late Event Window.
- **Rollups.** Per-(Customer, Dimension, Billing Period) aggregated totals. Recomputed when Late Events arrive.
- **Quota Thresholds & Alerts.** For each Metered Charge with a Quota, Saasy fires `usage.threshold.crossed` Webhooks at Integrator-configured Thresholds (defaults: `0.5`, `0.8`, `1.0`, `1.1`). State-based firing rule: each percentage fires once per period when the Rollup is at or above it. Thresholds added mid-period catch up immediately; removing stops future firing but does not retroactively un-fire ([ADR-0012](../decisions/ADR-0012-threshold-firing-rules.md)). Soft Enforcement only.
- **Invoice generation.** At **Final Close** (Period Close + Late Event Window), Saasy computes Line Items deterministically from the Subscription's Plan Version and the period's Rollups. Default Window 24h (per-Integrator override up to 7d). Output: structured JSON + HTML view (no PDF in v1). Disputes resolve via **Credit Note** flow. ([ADR-0010](../decisions/ADR-0010-period-boundaries-and-final-close.md))

### 4.2 Ingestion

Two channels feeding the same internal stream:

- **Event Hub (primary).** Saasy hosts a streaming bus, one namespace per region shared across Integrator Kinds. Per-Integrator credentials; partitioned by Integrator. Rollup workers consume via partition-aware checkpointing.
- **HTTP API (alternative).** `POST /v1/events` — single or batched. Idempotency via `idempotency_key`. Auth via per-Integrator API key.

Guarantees: at-least-once delivery; idempotency on `idempotency_key`; Events older than the Late Event Window rejected with a clear error.

External-bus connectors are out of scope for v1. Implementation in [ADR-0001](../decisions/ADR-0001-ingestion-stream.md).

### 4.3 Surfaces

- **Admin Dashboard** (Integrator-facing). Configure Product Types, Plans, Customers, Subscriptions; view Rollups; view Invoices; manage Webhooks and API keys. Bound to one Integrator; production and sandbox accounts log in separately.
- **Customer Portal** (Customer-facing). Embeddable iframe AND white-label standalone (e.g., `usage.acme.com`). Shows Subscription, Rollups vs Quotas, past Invoices, Plan Transition requests. Per-Integrator theming.
- **Ops Console** (Saasy-internal). Provision Integrators, system health, audit logs.

### 4.4 API & Webhooks

- REST API for all Integrator-facing operations (Product Types, Plans, Customers, Subscriptions, Events, Invoices, Webhooks, API keys).
- Webhook kinds: `usage.threshold.crossed`, `invoice.generated`, `subscription.changed`, `customer.created`. Generic `*` channel for testing. All payloads signed and replayable.
- SDK candidates (post-v1): Node, Python, Go, .NET.

## 5. Non-functional requirements

| Aspect | Target (v1) |
|---|---|
| Consistency | Eventual; usage reflected within ~30s under normal load |
| Ingestion throughput (per Integrator) | 1k Events/sec sustained, 10k/sec burst |
| Rollup freshness | Period-to-date counters refreshed every ≤30s |
| Invoice correctness | 100% reproducible — given the same Events and Plan Version, Invoice output is deterministic |
| Integrator isolation | Logical in v1; per-Integrator DB physical isolation post-v1 for enterprise tier |
| Auditability | Every Plan / Customer / Invoice change logged with actor + timestamp |
| Compliance | Designed-for-SOC2 from day one (audit logs, RBAC, encryption at rest); formal certification post-v1 |

## 6. Out of scope (v1)

- Payment collection (no Stripe / Adyen; no money movement)
- Tax computation (Avalara / Stripe Tax-style) — Invoices may carry a tax Line Item, but Saasy does not compute it
- PDF rendering of Invoices (HTML view only)
- Hot-path enforcement (synchronous allow / deny in the Integrator's request path)
- Revenue recognition / GAAP reporting
- Dunning / collections
- FX / multi-currency conversion
- Backdated Plan Transitions (effective in the past) — use Credit Notes for retroactive corrections
- Mid-window Invoice issuance with later corrections (Supplemental Invoices for Late Events) — Invoices issue at Final Close; Credit Notes are the only retroactive correction path

## 7. Phased delivery

| Phase | Scope |
|---|---|
| **0 — Foundation** | Integrators (Kind + Tier), Customers (`IntegratorOwned` Identity Mode), API keys, audit log skeleton, hosted Event Hub endpoint, HTTP ingestion endpoint |
| **1 — Metering MVP** | Product Types, simple Plans (Flat Fee + Per-Seat Fee + flat-rate Metered Charges), Rollups, Admin Dashboard read-only |
| **2 — Plans & Invoices** | Quota + Overage, Plan Transitions with Proration, Invoice generation, Webhooks (`usage.threshold.crossed`, `invoice.generated`) |
| **3 — Tiered pricing & Customer Portal** | Tiered Metered Charges (graduated + volume), embeddable Customer Portal, white-label theming |
| **4 — Identity Modes** | `Federated` (OIDC) Customer Portal mode + Admin Dashboard identity (ASP.NET Core Identity, per-Integrator OIDC SSO, MFA enforcement) per [ADR-0007](../decisions/ADR-0007-admin-dashboard-identity-and-customer-portal-posture.md) |
| **5 — Commitments & Credits** | Commitments, Minimums, Credit drawdown |
| **Post-v1** | Payments integration, multi-currency on one Customer, tax computation, per-Integrator DB isolation, SDKs, formal SOC2 |

## 8. Success metrics

- **v1 launch (post phase 3):** 3 paying design-partner Integrators; ≥1 billing real revenue through Saasy-generated Invoices.
- **Adoption:** time-to-first-Event < 30 minutes from API key issuance.
- **Reliability:** 99.9% Ingestion availability; zero Invoice computation errors per 10k Invoices.
- **Stickiness:** Integrator continues sending Events 30 days after first Event ≥ 80%.

## 9. Decision log

Tech-stack decisions live as ADRs under [docs/decisions](../decisions/).

| # | Decision | Where |
|---|---|---|
| A | Hosted ingestion stream → Azure Event Hubs | [ADR-0001](../decisions/ADR-0001-ingestion-stream.md) |
| B | Backend stack → .NET 10 + Aspire + ASP.NET Core | [ADR-0002](../decisions/ADR-0002-backend-stack.md) |
| C | Primary database → PostgreSQL on Azure (Flexible Server) | [ADR-0003](../decisions/ADR-0003-primary-database.md) |
| D | Frontend stack → React + Vite for all three UIs | [ADR-0004](../decisions/ADR-0004-frontend-stack.md) |
| E | Property-based testing → Conjecture (.NET) + fast-check (React) | [ADR-0005](../decisions/ADR-0005-property-based-testing.md) |
| F | NuGet via Central Package Management | [ADR-0006](../decisions/ADR-0006-central-package-management.md) |
| G | Per-context outbox + Internal Domain Event Bus on Service Bus; bounded context Delivery | [ADR-0008](../decisions/ADR-0008-internal-domain-event-bus.md) |
| H | Integrator-per-kind environment model: sandbox and production are separate Integrators | [ADR-0009](../decisions/ADR-0009-integrator-per-kind-environment-model.md) |
| I | Period boundaries in Integrator Timezone; anchor-day clamps to last-day-of-month; Final Close = Period Close + Late Event Window; reject Late Events past Final Close; default Window 24h, override up to 7d | [ADR-0010](../decisions/ADR-0010-period-boundaries-and-final-close.md) |
| J | Per-Seat Fee sources seat count from a `time_weighted_last` Dimension | [ADR-0011](../decisions/ADR-0011-per-seat-fee-time-weighted-aggregation.md) |
| K | Threshold firing is state-based with once-per-period record; catch-up fire on add via cross-context Domain Event | [ADR-0012](../decisions/ADR-0012-threshold-firing-rules.md) |
| L | `Saasy.SharedKernel.Domain` for cross-context Value Objects (`Money` + `Currency` in v1) | [ADR-0013](../decisions/ADR-0013-shared-kernel.md) |
| M | Customer-Subscription cardinality is zero-or-more by default; opt-in per-Plan exclusivity; Transitions stay within one Product Type | [ADR-0015](../decisions/ADR-0015-customer-subscription-cardinality.md) |
| N | Cross-context reads use event-driven Projections; bus carries rich state-change events; race window surfaces as HTTP 425 + Retry-After | [ADR-0016](../decisions/ADR-0016-event-driven-projections.md) |

## 10. Glossary

Canonical domain vocabulary lives in [CONTEXT.md](../../CONTEXT.md). All terms in this PRD are defined there with `_Avoid:_` lists.

## 11. Next steps

1. **Outstanding ADRs (all settled before phase 0 code):**
   - Admin Dashboard identity & Customer Portal posture — [ADR-0007](../decisions/ADR-0007-admin-dashboard-identity-and-customer-portal-posture.md).
   - Deployment topology — [ADR-0019](../decisions/ADR-0019-deployment-topology.md) (Azure Container Apps for Api + Worker).
   - Observability stack — [ADR-0018](../decisions/ADR-0018-observability-sinks.md) (OTel Collector → Application Insights).
   - Outbox retention + cold archive — [ADR-0017](../decisions/ADR-0017-outbox-retention-and-cold-archive.md).
   - Event Hubs Capture for raw-Event audit archive — [ADR-0020](../decisions/ADR-0020-event-hubs-capture.md).
2. **Phase 0 build:**
   - Keep [CONTEXT.md](../../CONTEXT.md) updated as new domain terms emerge.
   - Data-model sketch (EF Core entities for Integrator (Kind + Tier) / Customer / Plan / Plan Version / Subscription / Event / Rollup / Invoice).
   - Integrator + API-key skeleton.
   - HTTP `POST /v1/events` ingestion and Event Hub publish-only credential issuance.
3. **Design-partner outreach:** identify 2–3 candidate Integrators willing to validate phases 1–2 against real workloads.
