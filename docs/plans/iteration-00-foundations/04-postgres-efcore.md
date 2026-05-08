---
title: Postgres provisioning + EF Core schema-per-context
iteration: 00
status: todo
labels: [foundations, persistence, ef-core]
depends-on: [02-aspire-apphost]
agent: backend
---

# Postgres provisioning + EF Core schema-per-context wiring

Aspire provisions a single logical Postgres DB; each context's `DbContext` is configured to use its own schema. No tables yet — just the wiring.

## Acceptance criteria

- Aspire AppHost adds Postgres resource and passes the connection string to Api + Worker.
- `Saasy.Tenancy.Infrastructure` exposes a `TenancyDbContext` configured with `HasDefaultSchema("tenancy")`.
- A no-op migration runs on startup creating the schema and a sentinel table.
- Per-context `DbContext` registration helper documented for reuse in later iterations.

## References

- [ADR-0003](../../decisions/ADR-0003-primary-database.md)
- [docs/architecture/architecture.md](../../architecture/architecture.md)
