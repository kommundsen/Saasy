# Saasy.SharedKernel.Domain

Innermost project in the Saasy dependency graph. Contains only types that are shared across bounded context boundaries as typed objects in real flows.

## Contents

| Type | Description |
|---|---|
| `Currency` | ISO 4217 three-letter code with minor unit count. Immutable `readonly record struct`. Self-validates via static `Create(string code, int minorUnits)`. |
| `Money` | `(decimal Amount, Currency Currency)` immutable `sealed record`. Constructed via static `Create(decimal amount, Currency currency)`; the private constructor and the factory's non-negative check guarantee no `Money` instance ever carries a negative amount. Arithmetic (`Add`, `Subtract`, `Multiply`) throws `InvalidOperationException` on currency mismatch and on operations that would yield a negative result; `Multiply` rejects negative factors. No implicit rounding -- callers round as appropriate for the context. |

## Constraints

- Zero `<PackageReference>` entries. Only BCL.
- No reference to any bounded context Domain project.
- No context-specific behaviour may be added here; use a context-local type instead.

## Promotion rule (per ADR-0013)

A type may be added to this project only if **all four** of the following hold:

1. **Value Object.** Immutable, no identity, structural equality.
2. **Identical semantics across all consumers.** No context-specific variation in validation, behaviour, or arithmetic.
3. **Crosses context boundaries as a typed object in real flows**, where duplication forces lossy conversion or invites invariant drift.
4. **At least two bounded contexts genuinely need it.**

If any rule fails, the type belongs in the context that needs it, not here. Three types is a meaningful threshold; five would be a smell. Adding a type requires explicit review. Removing a type (when specialisation pressure grows) is the correct response.
