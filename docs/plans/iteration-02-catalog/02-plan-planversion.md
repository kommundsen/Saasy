---
title: Plan + PlanVersion (immutable versioning)
iteration: 02
status: todo
labels: [catalog, domain]
depends-on: [00-product-type-dimension]
agent: backend
---

# Plan + PlanVersion (immutable versioning)

`Plan` is a stable handle (id, name, ProductTypeId, currency). `PlanVersion` is immutable; any edit produces a new PlanVersion under the same Plan. PlanVersions carry the list of `PricingComponent`s. Plans and PlanVersions are separate Aggregate Roots per [docs/architecture/aggregates.md](../../architecture/aggregates.md).

## Acceptance criteria

- Strongly-Typed `PlanId`, `PlanVersionId`.
- `Plan.PublishNewVersion(components)` returns a new `PlanVersion` and stamps `PublishedAt`.
- `PlanVersion` exposes `PricingComponents` as a read-only collection.
- A Plan must always have at least one `Active` PlanVersion to be referenced by a Subscription; deactivation supported but does not affect existing PlanVersions.
- `IsExclusivePerCustomer` flag lives on Plan ([ADR-0015](../../decisions/ADR-0015-customer-subscription-cardinality.md)) — wiring is deferred to Iteration 03 but the field exists.
- Currency on Plan, fixed at first publish.
