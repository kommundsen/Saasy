---
title: Invoice + LineItem aggregates
iteration: 07
status: todo
labels: [invoicing, domain]
depends-on: []
---

# Invoice + LineItem aggregates

`Invoice` is the Aggregate Root in the Invoicing context. Owns `LineItem` Child Entities. One Invoice per `(SubscriptionId, Period)` — re-runs of generation must yield the same Invoice id and content (deterministic).

## Acceptance criteria

- Strongly-Typed `InvoiceId` derived deterministically from `(IntegratorId, SubscriptionId, PeriodStart, PeriodEnd)` (UUIDv5 over a stable namespace).
- `Invoice.Status: Draft | Issued | CreditedFully | CreditedPartial`.
- `LineItem`: `Description`, `Quantity`, `UnitPrice`, `Amount`, `Source` (PricingComponent reference), `PeriodStart`, `PeriodEnd`.
- Total = sum of LineItem amounts; currency invariant.
- Persistence under `invoicing` schema.
