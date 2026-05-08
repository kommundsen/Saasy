---
title: Read-only views (Tenancy, Catalog, Subscriptions, Audit)
iteration: 04
status: todo
labels: [frontend, admin-dashboard]
depends-on: [01-shared-ui-package, 02-openapi-client-generation]
agent: frontend
---

# Read-only views

Pages over the data shipped in Iterations 01–03.

## Acceptance criteria

- Pages:
  - `/integrators` — list, detail.
  - `/customers` — list, detail (per Integrator).
  - `/product-types` — list, detail (Dimensions inline).
  - `/plans` — list, detail (PlanVersions list, PricingComponents table).
  - `/subscriptions` — list, detail (current Rollups, status, history).
  - `/audit` — filterable audit log.
- All pages handle HTTP 425 gracefully with retry hint.
- Empty / error states designed.
- No write actions exposed.
