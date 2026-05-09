# Saasy.SharedKernel.Domain

Cross-context Value Objects shared by all bounded-context Domain layers. Zero NuGet references -- only the BCL.

## v1 contents

| Type | Purpose |
|---|---|
| `Currency` | ISO 4217 code + minor units; validates on `Create()` |
| `Money` | `(Amount, Currency)` with arithmetic that throws on currency mismatch |

## Promotion gate

A type may be added to this project only when **all four rules** hold (per [ADR-0013](../../docs/decisions/ADR-0013-shared-kernel.md)):

1. **Value Object.** Immutable, no identity, structural equality. Implemented as a C# `record` with a private constructor and static `Create()` factory.
2. **Identical semantics across all consumers.** No context-specific variation in validation, behaviour, or arithmetic. If even one context needs a specialised variant, the type stays out of the kernel -- define a separate type in that context instead.
3. **Crosses context boundaries as a typed object in real flows.** Duplication would force lossy conversion or cause invariant drift. Passing primitives at the seam is sufficient for types that only meet rules 1 and 2.
4. **At least two bounded contexts genuinely need it.**

Types that don't satisfy all four rules -- including Strongly-Typed IDs (which belong to one Aggregate Root in one context) and data shapes like `BillingPeriod` (same structure, no shared behaviour) -- are NOT eligible.

Removing a type is the correct response to specialisation pressure. The kernel stays small by default; five types would be a smell.
