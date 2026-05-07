---
title: Commitment aggregate + drawdown
iteration: 11
status: todo
labels: [subscriptions, billing, domain]
depends-on: []
---

# Commitment aggregate

`Commitment` is an Aggregate Root (or strongly-considered Child of Subscription — decision in this issue). Models a prepaid spend (e.g., $10k over 12 months) that is drawn down by Invoiced amounts before they hit the customer's payable total.

## Acceptance criteria

- Decision recorded in this file: AR vs. Child-of-Subscription. Default: separate Aggregate Root in Subscriptions context, since Commitments may span Plan Transitions.
- Fields: `IntegratorId`, `CustomerId`, `TermStart`, `TermEnd`, `Currency`, `PrepaidAmount: Money`, `DrawnDown: Money`, `Status: Active | Expired | Exhausted`.
- Drawdown is FIFO across overlapping commitments (oldest exhausts first).
- Rollover policy: none in v1 (commitments expire at TermEnd; remaining balance is logged but not refunded).
- API: create, get, list per customer; read-only after creation.
