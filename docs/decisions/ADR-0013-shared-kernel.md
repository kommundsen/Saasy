---
id: ADR-0013
type: adr
state: accepted
title: Saasy.SharedKernel.Domain for cross-context Value Objects
date: 2026-05-07
deciders: [kim]
---

# ADR-0013 — Saasy.SharedKernel.Domain

## Context

`Money` and `Currency` are referenced by aggregates in three contexts:

- **Tenancy.Customer** carries `Currency` (fixed ISO 4217 code).
- **Catalog.PlanVersion** and `PricingComponent`s carry `Money`.
- **Invoicing.Invoice** and `LineItem`s carry `Money`; Invoice carries `Currency`.

Two non-options:

- **Catalog owns them and others reference Catalog.Domain** — Catalog becomes a backdoor shared kernel. Tenancy.Customer would depend on Catalog.Domain to use `Currency`, inverting the natural dependency direction.
- **Each context redefines its own `Money` and `Currency`** — types are structurally equal but distinct, with conversion code at every seam. Worse, `Money`'s currency-mismatch arithmetic invariants would duplicate across contexts and silently drift.

`BillingPeriod` was considered and rejected: same `(Start, End)` shape across contexts but no shared behavior; cross-context use is at object creation time and can extract primitives at the seam.

## Decision

Introduce `Saasy.SharedKernel.Domain`. v1 contents: `Money` and `Currency` only.

### Project shape

- Zero NuGet references beyond the BCL. No references to any context's Domain.
- Every `Saasy.{Context}.Domain` references it; nothing else may redefine its types.
- Innermost layer in the dependency graph — even more upstream than any context's Domain.

### v1 contents

| Type | Why universal |
|---|---|
| `Currency` | ISO 4217 with three-letter validation. Used by Catalog, Invoicing, Tenancy. Identical validation everywhere. |
| `Money` | `(Amount, Currency)` with arithmetic (`Add`, `Subtract`, `Multiply`) that throws on currency mismatch. Crosses Catalog → Invoicing as a typed object (Pricing Component → Line Item snapshot). |

`Money` and `Currency` are co-located because `Money` depends on `Currency`.

### Promotion rule

A type is eligible for the Shared Kernel only if **all four** hold:

1. **Value Object.** Immutable, no identity, structural equality.
2. **Identical semantics across all consumers.** No context-specific variation in validation, behavior, or arithmetic.
3. **Crosses context boundaries as a typed object in real flows**, where duplication forces lossy conversion or invites invariant drift.
4. **At least two bounded contexts genuinely need it.**

NOT eligible: types with context-specific behavior, Strongly-Typed IDs (each ID belongs to one Aggregate Root in one context), data-only types whose contexts can each maintain a copy without invariant risk (e.g., `BillingPeriod`).

### Discipline

Adding a type requires explicit review. Removing a type is allowed and is the correct response to specialization pressure. The kernel stays small by default — three types is a meaningful threshold; five would be a smell.

## Consequences

- **Single source of truth for Money arithmetic.** Currency-mismatch invariants defined once.
- **No backdoor shared kernel through Catalog.** Dependency direction stays clean.
- **Onion direction preserved** — `SharedKernel.Domain` sits even more upstream than context Domains.
- **No conversion code at every seam** — Pricing Component's `Money` and Line Item's `Money` are the same type.
- **Cost: one more project.** Tiny.
- **Cost: cross-context coordination on kernel changes.** Intended — shared semantics should ripple — but makes changes deliberate.

### Out of scope

`Saasy.SharedKernel.Application` or `.Infrastructure` (none proposed); future kernel candidates (gated by the four-rule review); per-context `Money` specialization (use a separate type in that context, do not relax the kernel).
