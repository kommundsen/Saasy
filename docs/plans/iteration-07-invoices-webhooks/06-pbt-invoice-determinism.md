---
title: Property tests for invoice determinism
iteration: 07
status: todo
labels: [invoicing, testing, pbt]
depends-on: [01-invoice-generation-worker, 02-invoice-renderers]
agent: backend
---

# Property tests for invoice determinism

## Acceptance criteria

- Generators: arbitrary `(Period, PricingComponents, Rollup snapshots, Plan Transitions)` triples that satisfy invariants from earlier iterations.
- Properties:
  - **Aggregate determinism** — generating an Invoice from the same input twice yields identical aggregate state (same id, same LineItems, same Total).
  - **Renderer determinism** — JSON and HTML byte-for-byte equal across N runs.
  - **LineItem ordering** — total ordering on `(PricingComponentId, PeriodStart)` is total + stable.
  - **Currency invariant** — every LineItem currency equals Plan currency.
  - **Sum invariant** — `Total = sum(LineItems.Amount)`.
- CI seed pinned per [ADR-0005](../../decisions/ADR-0005-property-based-testing.md).
