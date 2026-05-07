---
title: Cross-context projections for Subscriptions context
iteration: 03
status: todo
labels: [subscriptions, projections]
depends-on: [02-service-bus-topology]
---

# Cross-context projections for Subscriptions context

Subscriptions needs to read minimal facts about Customers, Plans, PlanVersions, and Dimensions. Per [ADR-0016](../../decisions/ADR-0016-event-driven-projections.md), the context maintains its own projection tables populated by `ProjectionUpdater` BackgroundServices that consume Tenancy + Catalog events.

## Acceptance criteria

- Projection tables in `subscriptions` schema:
  - `customer_projections (customer_id, integrator_id, external_ref, display_name, deleted_at)`
  - `plan_projections (plan_id, integrator_id, product_type_id, currency, is_exclusive_per_customer, deactivated_at)`
  - `plan_version_projections (plan_version_id, plan_id, published_at)`
  - `dimension_projections (dimension_id, product_type_id, code, aggregation)`
- `ProjectionUpdater` services consume `tenancy.events` + `catalog.events` and upsert.
- Lag SLO: < 5 seconds in dev; HTTP 425 fallback on Subscription create when projection missing required record.
- Tests cover idempotent projection upserts under duplicate delivery.
