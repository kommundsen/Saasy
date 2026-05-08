---
title: Catalog API surface
iteration: 02
status: todo
labels: [catalog, api]
depends-on: [00-product-type-dimension, 02-plan-planversion, 03-pricing-components-phase1]
agent: backend
---

# Catalog API surface

Endpoints to manage Catalog from the Admin Dashboard or directly via the integration API.

## Acceptance criteria

- `POST /v1/product-types`, `POST /v1/product-types/{id}/dimensions` (immutable create — no PATCH on Dimension).
- `POST /v1/plans`, `POST /v1/plans/{id}/versions` (publishing a new PlanVersion).
- `GET` reads on all of the above.
- All endpoints require ApiKey auth and scope to `IntegratorId`.
- OpenAPI spec generated; written to `openapi/catalog.yaml`.
