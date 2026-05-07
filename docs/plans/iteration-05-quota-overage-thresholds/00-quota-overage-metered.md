---
title: Quota + Overage on MeteredCharge
iteration: 05
status: todo
labels: [catalog, pricing, domain]
depends-on: []
---

# Quota + Overage on MeteredCharge

Extend the `MeteredCharge` Pricing Component with `QuotaPerPeriod` and `OverageRate`. Calculation:
- Within Quota → free (covered by Flat Fee or Per-Seat Fee elsewhere).
- Above Quota → `(usage − Quota) × OverageRate` per Period.

## Acceptance criteria

- `MeteredCharge` value object includes `QuotaPerPeriod: decimal` and `OverageRate: Money?`.
- Validation: if `QuotaPerPeriod = 0`, `OverageRate` becomes the rate for all units; if `QuotaPerPeriod > 0` then `OverageRate` is required.
- Money currency on `OverageRate` must equal Plan currency.
- Existing flat-rate construct (no quota) continues to work.
- Unit tests for boundary at exact quota and just-over.
