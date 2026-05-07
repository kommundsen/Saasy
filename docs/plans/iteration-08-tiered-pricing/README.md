# Iteration 08 — Tiered Pricing

## Goal

Extend `MeteredCharge` with two tier ladder types: **Graduated** and **Volume**. Tier definitions live on the Pricing Component; pricing logic computes line-item amounts from a Rollup's `AggregatedValue` against the ladder.

## Out of scope

No structural changes to Invoice/LineItem layout — tiered Pricing Components emit a sequence of LineItems (or a single LineItem with breakdown in `Description`, decided in this iteration).

## Exit criteria

- `MeteredCharge` Pricing Component supports `RateModel: FlatRate | Graduated | Volume`.
- Graduated: each tier rate applied to its own slice of usage.
- Volume: a single tier rate applied to all usage based on the tier the total falls into.
- Tier ladder validation: ascending bounds, no gaps, last tier may be unbounded.
- LineItem rendering convention chosen and documented.
- Property tests for tier monotonicity, boundary correctness, and revenue equivalence at boundary.

## Issues

- [ ] [00 — Tier ladder value object (Graduated, Volume)](00-tier-ladder.md)
- [ ] [01 — Pricing engine extension for Graduated + Volume](01-pricing-engine-tiered.md)
- [ ] [02 — LineItem rendering convention for tiered charges](02-line-item-rendering.md)
- [ ] [03 — Property tests for tier ladders](03-pbt-tier-ladders.md)
