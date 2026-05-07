---
title: LineItem rendering convention for tiered charges
iteration: 08
status: todo
labels: [invoicing]
depends-on: [01-pricing-engine-tiered]
---

# LineItem rendering convention

Decide and document: do `Graduated` ladders produce one LineItem per tier band hit, or one summary LineItem with the breakdown in `Description`? Volume produces a single LineItem either way.

## Acceptance criteria

- Decision documented in this issue file once chosen.
- Recommendation: one LineItem per tier band actually hit, plus a single summary LineItem if the renderer prefers (HTML) — this gives a queryable breakdown in JSON while keeping HTML readable.
- Update Invoice renderers to honor the convention.
