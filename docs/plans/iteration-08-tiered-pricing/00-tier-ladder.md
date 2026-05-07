---
title: Tier ladder value object (Graduated, Volume)
iteration: 08
status: todo
labels: [catalog, pricing, domain]
depends-on: []
---

# Tier ladder value object

`TierLadder` is a value object: an ordered sequence of `Tier(upTo: decimal?, unitRate: Money)` plus a `Mode: Graduated | Volume`. The last `upTo = null` denotes unbounded.

## Acceptance criteria

- Validation: bounds are strictly ascending; only the last may be unbounded; all `unitRate` currencies match.
- Quota interaction: Quota subtracts from usage **before** ladder evaluation (tier ladder applies to overage usage only when Quota > 0).
- Serialization stable for migration safety.
- Unit tests for invalid ladders.
