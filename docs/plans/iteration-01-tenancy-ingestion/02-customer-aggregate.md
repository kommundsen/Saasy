---
title: Customer aggregate
iteration: 01
status: todo
labels: [tenancy, domain]
depends-on: [00-integrator-aggregate]
agent: backend
---

# Customer aggregate

`Customer` is an Aggregate Root scoped under an `Integrator`. `IntegratorId` is required and immutable post-creation. No subscription linkage yet.

## Acceptance criteria

- Strongly-Typed `CustomerId`.
- Factory `Customer.Create(integratorId, externalRef, displayName)` requires non-empty `externalRef` (Integrator's own customer key, unique per Integrator).
- Unique constraint `(IntegratorId, ExternalRef)` enforced in DB.
- `Customer.Rename(displayName)` emits `CustomerRenamed`.
- API endpoints: `POST /v1/customers`, `GET /v1/customers/{id}`, `PATCH /v1/customers/{id}`.
