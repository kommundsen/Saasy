---
title: Integrator aggregate (Kind, Tier, Timezone)
iteration: 01
status: todo
labels: [tenancy, domain]
depends-on: []
agent: backend
---

# Integrator aggregate

`Integrator` is an Aggregate Root in the Tenancy context. `Kind` (`sandbox` | `production`) and `Tier` are set at creation; `Kind` is immutable, `Tier` is mutable per [ADR-0009](../../decisions/ADR-0009-integrator-per-kind-environment-model.md). `Timezone` is set at creation and used everywhere Period Boundaries apply ([ADR-0010](../../decisions/ADR-0010-period-boundaries-and-final-close.md)).

## Acceptance criteria

- Strongly-Typed `IntegratorId`.
- Static factory `Integrator.Create(name, kind, tier, timezone)` validates IANA timezone and Kind/Tier values.
- `ChangeTier` domain method emits a `IntegratorTierChanged` Domain Event; `ChangeKind` does not exist.
- EF Core mapping in `Saasy.Tenancy.Infrastructure` with `tenancy` schema.
- Unit tests cover create + tier-change paths.

## References

- [ADR-0009](../../decisions/ADR-0009-integrator-per-kind-environment-model.md)
- [ADR-0010](../../decisions/ADR-0010-period-boundaries-and-final-close.md)
- [docs/architecture/aggregates.md](../../architecture/aggregates.md) — Tenancy section
