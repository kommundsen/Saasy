# Iteration 11 — Commitments, Minimums & Credits

## Goal

Add the three advanced billing constructs from PRD Phase 5:
- **Commitment** — pre-purchased usage or revenue, drawn down over a Commitment Term.
- **Minimum** — per-Billing-Period revenue floor; if computed Invoice falls short, a top-up LineItem is added.
- **Credit** — prepaid balance applied to Invoices before final amount due.

Each interacts with Invoice generation; sequencing is critical (Credits apply last).

## Out of scope

No payments. No refunds (handled via CreditNote from Iteration 07).

## Exit criteria

- `Commitment` aggregate in Subscriptions context: term, prepaid amount or unit count, drawdown rules.
- `Minimum` is a Pricing Component on PlanVersion: `MinimumPerPeriod: Money`.
- `CreditBalance` aggregate per Customer: deposits, expiries, applications.
- Invoice generation pipeline updated to: compute LineItems → apply Commitment drawdown → apply Minimum top-up → apply Credit drawdown → write Invoice.
- Property tests for ordering invariance (within ordering rules), conservation (drawdowns sum to consumption), monotonicity.

## Issues

- [ ] [00 — Commitment aggregate + drawdown](00-commitment.md)
- [ ] [01 — Minimum Pricing Component + top-up LineItem](01-minimum-topup.md)
- [ ] [02 — CreditBalance aggregate + application](02-credit-balance.md)
- [ ] [03 — Invoice pipeline ordering: LineItems → Commitment → Minimum → Credit](03-invoice-pipeline-ordering.md)
- [ ] [04 — Property tests (ordering, conservation, monotonicity)](04-pbt-advanced-billing.md)
- [ ] [05 — Documentation & domain glossary updates in CONTEXT.md](05-context-glossary-updates.md)
