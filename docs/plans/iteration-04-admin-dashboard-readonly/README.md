# Iteration 04 — Admin Dashboard (read-only)

## Goal

Stand up the React + Vite Admin Dashboard with read-only views over Integrators, Customers, Product Types, Plans, PlanVersions, Subscriptions, and current Rollups. Authentication uses an `integrator-owned` placeholder token model — proper Identity Modes land in Iteration 10.

## Out of scope

No write paths from the UI. No Customer Portal. No Ops Console. No theming customization.

## Exit criteria

- React + Vite app `apps/admin-dashboard` boots, builds, and is referenced by Aspire AppHost as a static-served SPA.
- OpenAPI client generated from `openapi/*.yaml` into `packages/api-client`.
- Shared component library scaffold in `packages/ui` (per [ADR-0004](../../decisions/ADR-0004-frontend-stack.md)).
- Pages: integrator list/detail, customers list/detail, product types + dimensions, plans + plan versions, subscriptions list/detail with Rollups, audit log viewer.
- Frontend property tests using `fast-check` for at least one critical formatting helper (e.g. `Money` display).
- `OpenTelemetry` collector decision recorded so dashboards exist for support use.

## Issues

- [ ] [00 — React + Vite scaffold + Aspire integration](00-react-vite-scaffold.md)
- [ ] [01 — Shared UI package + design tokens](01-shared-ui-package.md)
- [ ] [02 — OpenAPI client generation pipeline](02-openapi-client-generation.md)
- [ ] [03 — Read-only views (Tenancy, Catalog, Subscriptions, Audit)](03-readonly-views.md)
- [ ] [04 — Frontend property tests with fast-check](04-frontend-pbt.md)
- [ ] [05 — Observability sink decision (recorded as ADR)](05-observability-sink-adr.md)
