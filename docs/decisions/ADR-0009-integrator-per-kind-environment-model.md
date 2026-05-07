---
id: ADR-0009
type: adr
state: accepted
title: Integrator-per-kind environment model
date: 2026-05-06
deciders: [kim]
---

# ADR-0009 — Integrator-per-kind environment model

## Context

Earlier drafts modeled "Environment" (`sandbox` / `production`) as a property of every aggregate, scoped via an `environment_name` column. Three weaknesses:

1. **Enforcement is purely code discipline.** EF Core query filters help but are silently bypassed by `IgnoreQueryFilters()`, raw SQL, ad-hoc support queries, and any background job that doesn't set the environment context. A single missed predicate leaks production into sandbox or vice versa.
2. **Conceptual duplication.** The Integrator was already the data-isolation boundary; Environment as a parallel boundary inside that meant every aggregate carried two scoping concerns where one would do.
3. **`ApiKey` complications.** Per-Environment API keys forced `EnvironmentName` onto a Child Entity that had no other reason to know about Environment.

The decisive alternative: **collapse Environment into the Integrator boundary itself.** A sandbox Integrator and a production Integrator are completely separate Integrators with no shared data. Matches Salesforce sandboxes, separate Azure tenants, GitHub orgs for staging, historic Stripe accounts.

## Decision

Adopt the **Integrator-per-kind** model. Environment ceases to exist as a separate concept on any aggregate.

### Integrator carries a Kind

- `IntegratorKind` is a Value Object with two values: `production` and `sandbox`. Fixed at creation; never mutated. Promoting a sandbox Integrator to production is not supported (the customer creates a new production Integrator and uses tooling to copy configuration).
- `Integrator.Kind` is the only place Environment-like information lives.
- Anything non-production is `sandbox`. Multiple sandbox Integrators can be named for distinct dev / qa / staging spaces (`acme-dev`, `acme-qa`).

### Limits live in a separate Tier concept

- `IntegratorTier` is a Value Object decoupled from Kind, capturing rate limits, ingestion ceilings, retention windows, feature flags.
- Defaults: `production` → paid tier; `sandbox` → free, capped tier. Decoupling allows a sandbox Integrator on an enterprise account to be granted higher-than-default sandbox limits without changing its Kind.

### No environment scoping on aggregates

- Every aggregate's table loses any `environment_name` column.
- ApiKey loses its `EnvironmentName` field.
- EF Core query filters for environment scoping go away; `IntegratorId` filtering was already required.
- Aspire AppHost provisions one schema per bounded context per region, not two-per-context.
- The UIs do not have an "environment toggle" — the Integrator the user is authenticated against IS the environment.

### No sandbox→production link in v1

v1 ships with no `ParentProductionIntegratorId` field. Promotion of a Plan from sandbox to production is a manual flow in the Admin Dashboard (export config / import to a target Integrator). A soft sibling-link can be added later if design partners ask for one-click linked promotion.

### Infrastructure topology

- **One Event Hubs namespace per region** (revises [ADR-0001](ADR-0001-ingestion-stream.md)). Per-Integrator credentials, partition key = `integrator_id`.
- **One Service Bus namespace per region** (per [ADR-0008](ADR-0008-internal-domain-event-bus.md)). Topics per emitting context, sessions keyed by `integrator_id`.
- **One Postgres database per region** (per [ADR-0019](ADR-0019-deployment-topology.md)). Per-Integrator data isolation is by `IntegratorId`; per-Integrator-DB physical isolation is a post-v1 enterprise-tier concern.

## Consequences

### Wins

- **Cross-environment leakage is structurally impossible.** Sandbox and production data live under different Integrator IDs. There is no shared row or shared key.
- **Aggregate model simplifies.** Drop `EnvironmentName` from ApiKey; drop env-scoping columns; drop the "Environment is NOT an aggregate" reasoning.
- **Operational surface halves vs schema-per-env / DB-per-env alternatives.** One namespace each per region, one schema per bounded context.
- **Cleanest path to physical isolation later.** When the post-v1 enterprise tier wants per-Integrator-DB isolation, moving one Integrator's data is straightforward — no co-tenant data to extract.

### Costs

- **Two-account UX for design partners.** Product/eng logs into a sandbox account and a production account separately rather than toggling a "test mode" switch. Mitigated by clear naming and the manual promotion flow.
- **No automatic config promotion.** Manual export/import in v1; one-click linked promotion is post-v1.
- **`Integrator.Kind` cannot change.** Promoting a sandbox to production is exactly the kind of structural move that breaks isolation guarantees if allowed.
- **Tier admin lives on the Integrator.** Ops Console must surface and manage `IntegratorTier` per Integrator.

### Out of scope

`IntegratorTier` catalog and limits; soft sibling-link between production and sandbox Integrators; per-Integrator physical isolation; whether Saasy charges Integrators differently for sandbox vs production usage.
