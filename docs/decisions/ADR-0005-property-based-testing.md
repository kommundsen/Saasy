---
id: ADR-0005
type: adr
state: accepted
title: Property-based testing — Conjecture (.NET) + fast-check (React)
date: 2026-05-03
deciders: [kim]
---

# ADR-0005 — Property-based testing

## Context

Saasy carries domain logic where edge-case correctness is load-bearing — invoice determinism (PRD §5: "given the same Events and Plan Version, Invoice output is deterministic"), Rollup aggregation under Late Events, Plan Transition with daily Proration, tier ladder pricing, Threshold crossing detection, Currency arithmetic. Example-based tests cover the cases the author thinks of; property-based testing exercises a generated cross-product of inputs and shrinks failures to minimal counterexamples.

The author maintains [Conjecture](https://ommundsen.dev/Conjecture/), a Hypothesis-style PBT library for .NET. fast-check is the de-facto PBT library in JS/TS.

## Decision

Adopt PBT as a first-class testing approach.

- **.NET:** **Conjecture** with the xUnit.V3 adapter. Test attribute is `[Property]`. `[Arbitrary]` source-generates `IStrategyProvider<T>` for key domain types (`PlanVersion`, `Subscription`, `Event`, `BillingPeriod`).
- **React:** **fast-check** with `@fast-check/vitest`.

### Conjecture packages used

| Package | For |
|---|---|
| `Conjecture.Core`, `Conjecture.Xunit.V3`, `Conjecture.Generators`, `Conjecture.Analyzers` | Base library + xUnit adapter |
| `Conjecture.Aspire`, `Conjecture.Aspire.EFCore`, `Conjecture.Aspire.Http` | Aspire-orchestrated test rigs |
| `Conjecture.AspNetCore`, `Conjecture.AspNetCore.EFCore` | ASP.NET Core endpoint properties |
| `Conjecture.EFCore` | Mapping round-trip properties against a Postgres test container |
| `Conjecture.Money`, `Conjecture.Time` | Strategies for monetary / time inputs |
| `Conjecture.Interactions` | State-machine PBT for Subscription lifecycle sequences |
| `Conjecture.OpenApi`, `Conjecture.JsonSchema`, `Conjecture.Regex` | Spec-derived and pattern Strategies |

### Where PBT applies

- **Catalog** — Plan Version composition, Tier ladders, Currency arithmetic (`Conjecture.Money`).
- **Subscriptions** — state machine, Plan Transition Policy, Proration (`Conjecture.Interactions`, `Conjecture.Time`).
- **Invoicing** — Invoice determinism, Credit Note math (`Conjecture.Money`, `Conjecture.Time`).
- **Metering** — Rollup aggregation under all Event orderings + Late Events; Threshold crossing detection.
- **Tenancy** — Slug uniqueness, API key lifecycle, Customer Currency invariants.
- **Api** — REST round-trip + contract invariants (`Conjecture.AspNetCore` + `Conjecture.OpenApi`).
- **Persistence** — EF Core round-trip + idempotency (`Conjecture.EFCore` or `Conjecture.Aspire.EFCore`).
- **Frontend** — pricing renderers, currency formatters, signed-token parsers (fast-check + custom arbitraries).

PBT does NOT apply to E2E tests, UI snapshots, or observability tests.

### Configuration

- **PR CI uses each engine's standard policy.** No `MaxExamples` / `numRuns` overrides; let Conjecture and fast-check pick their defaults so the schedule varies across runs and exploration grows over time.
- **Nightly correctness suites override** — invoice / Rollup / pricing-renderer / token-parser suites set `MaxExamples = 1000+` (Conjecture) and `numRuns = 1000` (fast-check) at the assembly / suite level.
- **No per-`[Property]` `Seed` overrides.** Reproducibility on failure comes from `Database` (Conjecture: failing IRs persist to `.conjecture/examples/` and replay on the next run) and exported Reproductions (see below). Pinning a per-test seed freezes the schedule and recreates the seeded-`[Theory]` anti-pattern.
- `Targeting` (Conjecture) where useful (e.g., maximize Invoice line-item count).

### Strategy reuse beyond `[Property]` tests

The same `[Arbitrary]`-decorated types feed integration-test fixtures, demo / sandbox seeding, and load tests. One Strategy per domain type, used everywhere.

### Reproductions

CI failures export Reproductions to `.conjecture/repros/` and become regression tests.

### Gap

No `Conjecture.Messaging.AzureEventHubs` exists; Saasy authors local Strategies/Interactions on top of `Conjecture.Messaging`. Contributing the package upstream is a clean follow-up if it proves valuable.

### Reference

[ommundsen.dev/Conjecture](https://ommundsen.dev/Conjecture/) is the canonical reference. The `conjecture` MCP server (configured in `.mcp.json`) is authoritative for any Conjecture question — prefer it over internal knowledge.

## Consequences

- **Correctness uplift on the highest-stakes logic.** Invoice determinism, Rollup correctness, and Proration math are exactly the surfaces where PBT pays back fastest.
- **CI time.** PR CI runs PBT at low `MaxExamples`; nightly CI runs higher and reports separately.
- **Vocabulary.** Test code uses Conjecture vocabulary verbatim: **Property**, **Example**, **Status**, **Strategy**, **Shrink**, **Reproduction**.
- **Learning curve.** Engineers unfamiliar with PBT need to learn invariant-thinking and shrunk counterexamples. Pair-program the first property test in each project.
- **Risk: Conjecture maturity.** Author can fix it, but pin versions and avoid preview APIs.
