---
title: Pricing engine extension for Graduated + Volume
iteration: 08
status: todo
labels: [invoicing, pricing]
depends-on: [00-tier-ladder]
---

# Pricing engine — Graduated + Volume

Pure functions over `(usage, TierLadder)` returning `(perTierBreakdown, totalAmount)` for use by Invoice generation.

## Acceptance criteria

- `Graduated`: for usage `u` and tier bounds `b1 < b2 < ...`, charge for each tier the portion of `u` that falls within it.
- `Volume`: locate the tier containing `u`, charge `u × tier.unitRate`.
- Functions are pure and currency-safe (`Money` arithmetic).
- Documented rounding rule per LineItem (banker's vs. half-up — pick one, codify in ADR if not already).
