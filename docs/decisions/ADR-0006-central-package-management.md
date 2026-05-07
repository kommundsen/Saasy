---
id: ADR-0006
type: adr
state: accepted
title: Central Package Management for all NuGet dependencies
date: 2026-05-03
deciders: [kim]
---

# ADR-0006 — Central Package Management

## Context

Saasy will land many .NET projects (API host, workers, AppHost, per-context Domain/Application/Infrastructure trios, matching `*.Tests`). Without a single source of truth for NuGet versions, .csproj files drift and reproducible builds erode. Microsoft's recommended convention is **Central Package Management** (CPM): one `Directory.Packages.props` at the repo root, version-less `<PackageReference>` in each .csproj.

## Decision

Use **CPM** for all NuGet dependencies.

- A single `Directory.Packages.props` at the repo root with `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` and one `<PackageVersion>` entry per package.
- Individual .csproj files use `<PackageReference Include="..." />` with no `Version` attribute.
- Versions bumped via PR to `Directory.Packages.props` only.
- Group `<PackageVersion>` entries by concern (Microsoft / Aspire, EF Core, Azure SDKs, Conjecture, test frameworks, observability) for readability.
- Transitive pinning is OFF by default; enable selectively if `NU1605` downgrade warnings appear.

## Consequences

- **One place to bump.** Dependabot / Renovate PRs touch only `Directory.Packages.props`.
- **Reproducible builds.** All projects compile against identical versions.
- **Conjecture package family stays lockstep** on upgrades (per [ADR-0005](ADR-0005-property-based-testing.md), ~16 packages).
- **Per-project override** via `<PackageVersion ... VersionOverride="..." />` exists as an escape hatch.
