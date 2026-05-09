---
title: Central Package Management bootstrap
iteration: 00
status: done
labels: [foundations, repo]
depends-on: [00-solution-layout]
agent: backend
---

# Central Package Management bootstrap

Configure CPM so all NuGet versions live in `Directory.Packages.props` and `csproj` files reference packages without versions.

## Acceptance criteria

- `Directory.Packages.props` exists at repo root with `ManagePackageVersionsCentrally=true`.
- All `<PackageReference>` entries in csprojs omit `Version=`.
- Renovate or Dependabot config touches only `Directory.Packages.props`.
- `dotnet restore` succeeds clean.

## References

- [ADR-0006](../../decisions/ADR-0006-central-package-management.md)
