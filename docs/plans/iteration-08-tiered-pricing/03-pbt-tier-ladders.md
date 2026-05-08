---
title: Property tests for tier ladders
iteration: 08
status: todo
labels: [invoicing, testing, pbt]
depends-on: [01-pricing-engine-tiered]
agent: backend
---

# Property tests for tier ladders

## Acceptance criteria

- Generators: arbitrary valid TierLadders; arbitrary usage values.
- Properties:
  - **Monotone in usage** — for the same ladder, increasing `u` never decreases `totalAmount`.
  - **Boundary continuity (Graduated)** — `total(u)` is continuous at tier boundaries.
  - **Volume jumps are non-negative** — at a Volume boundary, `total(b+ε) >= total(b−ε)` (Volume is allowed to jump).
  - **Equivalence at single-tier ladder** — Graduated and Volume produce identical totals.
  - **Revenue invariance under split** — for any usage `u`, splitting the ladder by inserting a redundant boundary does not change `total(u)` for Graduated.
