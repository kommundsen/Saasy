---
title: Frontend property tests with fast-check
iteration: 04
status: todo
labels: [frontend, testing, pbt]
depends-on: [01-shared-ui-package]
agent: frontend
---

# Frontend property tests with fast-check

Per [ADR-0005](../../decisions/ADR-0005-property-based-testing.md), use `fast-check` for non-trivial pure logic in the SPA. First target: `MoneyDisplay` formatting helper.

## Acceptance criteria

- `packages/ui/src/MoneyDisplay.test.ts` with fast-check arbitraries for `Money`.
- Properties:
  - Round-trip parse/format yields the same `Money`.
  - Output respects `Currency` minor units.
  - Locale switching does not change numeric value, only presentation.
- Runs in CI alongside backend tests.
