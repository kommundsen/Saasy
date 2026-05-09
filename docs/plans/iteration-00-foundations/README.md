# Iteration 00 — Foundations

## Goal

Stand up the empty-but-runnable solution: Aspire AppHost composing Api + Worker projects, Postgres locally, SharedKernel built and referenced by per-context projects, Conjecture.NET wired into a sample test project, OpenTelemetry baseline, CI green on `main`.

## Out of scope

No domain logic. No HTTP endpoints beyond a health probe. No actual ingestion. No UI.

## Exit criteria

- `dotnet run --project Saasy.AppHost` brings up Api + at least one BackgroundService Worker plus Postgres via Aspire.
- `Directory.Packages.props` is the single source of NuGet versions ([ADR-0006](../../decisions/ADR-0006-central-package-management.md)).
- `Saasy.SharedKernel.Domain` is published as a project reference, contains `Money` + `Currency` only, and has zero NuGet dependencies ([ADR-0013](../../decisions/ADR-0013-shared-kernel.md)).
- One per-context project (e.g. `Saasy.Tenancy.Domain` placeholder) compiles against SharedKernel.
- Conjecture.NET project exists and runs at least one passing property test against `Money`.
- CI pipeline runs `dotnet build`, `dotnet test`, and lint on every PR; green on `main`.
- OpenTelemetry traces flow into the Aspire dev dashboard.

## Issues

- [x] [00 — Solution layout & per-context project conventions](00-solution-layout.md)
- [x] [01 — Central Package Management bootstrap](01-central-package-management.md)
- [x] [02 — Aspire AppHost composition root](02-aspire-apphost.md)
- [ ] [03 — SharedKernel: Money & Currency](03-shared-kernel-money-currency.md)
- [ ] [04 — Postgres provisioning via Aspire + EF Core schema-per-context wiring](04-postgres-efcore.md)
- [ ] [05 — Conjecture.NET test project scaffold](05-conjecture-test-scaffold.md)
- [ ] [06 — OpenTelemetry baseline](06-otel-baseline.md)
- [ ] [07 — CI pipeline (build, test, lint)](07-ci-pipeline.md)
