---
title: Solution layout & per-context project conventions
iteration: 00
status: done
labels: [foundations, repo]
depends-on: []
agent: backend
---

# Solution layout & per-context project conventions

Establish the folder + project structure that every later iteration will extend. Each Bounded Context owns its own set of projects; SharedKernel is its own assembly; Aspire AppHost references Api + Worker hosts.

## Acceptance criteria

- `src/` contains: `Saasy.AppHost`, `Saasy.Api`, `Saasy.Worker`, `Saasy.SharedKernel.Domain`, and one placeholder context cluster `Saasy.Tenancy.Domain` / `Saasy.Tenancy.Infrastructure` / `Saasy.Tenancy.Application`.
- `tests/` mirrors `src/` with `*.Tests` and `*.PropertyTests` projects.
- README documents the convention: `Saasy.<Context>.<Layer>` naming, layer dependency rules per [architecture.md](../../architecture/architecture.md).
- `dotnet build` succeeds with zero warnings.

## References

- [docs/architecture/architecture.md](../../architecture/architecture.md) §layers
- [ADR-0013](../../decisions/ADR-0013-shared-kernel.md)
