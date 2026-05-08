---
title: Invoice pipeline ordering — LineItems → Commitment → Minimum → Credit
iteration: 11
status: todo
labels: [invoicing, billing]
depends-on: [00-commitment, 01-minimum-topup, 02-credit-balance]
agent: backend
---

# Invoice pipeline ordering

Document and enforce the order in which adjustments apply during Invoice generation:

1. Compute base LineItems from Pricing Components (FlatFee, PerSeatFee, MeteredCharge incl. tiers).
2. Apply **Commitment** drawdown — reduces the customer-payable subtotal but the gross LineItems remain visible (drawdown shown as a negative-amount LineItem with Description "Commitment drawdown").
3. Apply **Minimum** top-up — if subtotal-after-commitment < MinimumPerPeriod, add top-up LineItem.
4. Apply **Credit** drawdown — reduces final Total; appears as a negative-amount LineItem.

## Acceptance criteria

- Pipeline implemented as a sequence of pure transformations on an `InvoiceDraft` value.
- Ordering enforced (cannot apply Credit before Minimum, etc.).
- Each adjustment emits its own LineItem so the JSON Invoice fully explains the math.
- Re-running the pipeline on the same input is deterministic.
- Documented in [docs/architecture/architecture.md](../../architecture/architecture.md) "Invoice generation pipeline" section.
