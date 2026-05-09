---
title: Conjecture.NET test project scaffold
iteration: 00
status: done
labels: [foundations, testing]
depends-on: [03-shared-kernel-money-currency]
agent: backend
---

# Conjecture.NET test project scaffold

Stand up a property-test project referencing Conjecture.NET. Add a sample property test on `Money` to prove the harness works in CI.

## Acceptance criteria

- `Saasy.SharedKernel.Domain.PropertyTests` project exists.
- One generator for `Money` with bounded amounts.
- One property: `(a + b) + c == a + (b + c)` for matching currencies.
- Failures shrink to a minimal counterexample (validate by temporarily breaking the impl).
- CI runs property tests with a fixed seed for reproducibility.

## References

- [ADR-0005](../../decisions/ADR-0005-property-based-testing.md)
- Conjecture docs at https://ommundsen.dev/Conjecture/ (use `conjecture` MCP server)
