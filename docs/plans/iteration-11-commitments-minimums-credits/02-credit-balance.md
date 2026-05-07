---
title: CreditBalance aggregate + application
iteration: 11
status: todo
labels: [billing, domain]
depends-on: []
---

# CreditBalance aggregate

`CreditBalance` is per Customer (per currency). Deposits add to the balance; Invoice generation draws down. Optional expiry per deposit (FIFO consumption ignoring expired entries).

## Acceptance criteria

- Per-Customer, per-Currency `CreditBalance` aggregate.
- `Deposit(amount, expiresAt?)` appends a `CreditEntry`.
- `Apply(invoiceId, amount)` consumes from oldest unexpired entries first.
- Cannot result in negative `Total`; partial application supported.
- Refunds out of scope; reversals via Saasy operator action only (audit-logged).
