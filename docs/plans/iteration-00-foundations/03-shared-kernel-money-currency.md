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

- `Money(decimal Amount, Currency Currency)` immutable record-struct.
- `Currency` is an ISO-4217 enum or value object (3-letter code + minor units).
- Arithmetic ops require currency match; mismatch throws.
- Zero `<PackageReference>` entries in csproj.
- README in project documents the promotion rule per [ADR-0013](../../decisions/ADR-0013-shared-kernel.md).
- Unit + property tests cover commutativity, associativity, currency-mismatch, rounding.

## References

- [ADR-0013](../../decisions/ADR-0013-shared-kernel.md)
