---
title: Property tests (ordering, conservation, monotonicity)
iteration: 11
status: todo
labels: [invoicing, testing, pbt]
depends-on: [03-invoice-pipeline-ordering]
---

# Property tests for advanced billing

## Acceptance criteria

- Generators: arbitrary base LineItems, arbitrary Commitments (with overlap), arbitrary Minimum, arbitrary CreditBalance state.
- Properties:
  - **Conservation (Commitment)** — total drawdown across all Invoices ≤ original PrepaidAmount; cumulative drawdown is monotone non-decreasing.
  - **Conservation (Credit)** — total credit applied ≤ deposits minus expiries; cumulative is monotone non-decreasing.
  - **Minimum floor** — `Total - CreditApplied >= MinimumPerPeriod` whenever Minimum is configured (modulo Credit being able to push below — codify rule per [03](03-invoice-pipeline-ordering.md)).
  - **Determinism** — same inputs → same Invoice (id, LineItems, totals).
  - **Single-source of LineItems** — every adjustment LineItem has a `Source` discriminator that uniquely identifies which pipeline step produced it.
