---
title: SharedKernel — Money & Currency
iteration: 00
status: done
labels: [foundations, shared-kernel, domain]
depends-on: [00-solution-layout]
agent: backend
---

# SharedKernel — Money & Currency

Implement `Saasy.SharedKernel.Domain` containing only `Money` and `Currency` value objects. No NuGet dependencies. Strict promotion rule documented for adding any future type.

## Acceptance criteria

- `Money` is an immutable `sealed record` (class) of `(decimal Amount, Currency Currency)`. Construction goes through a static `Money.Create(decimal, Currency)` factory that rejects negative amounts; the constructor is `private` so the invariant cannot be bypassed (per architecture.md "Money amounts cannot be negative on construction").
- `Currency` is an ISO-4217 value object (3-letter code + minor units), self-validating via `Currency.Create(...)`.
- Arithmetic ops require currency match; mismatch throws. `Subtract` throws if it would yield a negative amount; `Multiply` throws on negative factors.
- Zero `<PackageReference>` entries in csproj.
- README in project documents the promotion rule per [ADR-0013](../../decisions/ADR-0013-shared-kernel.md).
- Unit + property tests cover commutativity, associativity, currency-mismatch, rounding.

## References

- [ADR-0013](../../decisions/ADR-0013-shared-kernel.md)
