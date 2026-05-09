---
title: CI pipeline (build, test, lint)
iteration: 00
status: done
labels: [foundations, ci]
depends-on: [05-conjecture-test-scaffold]
agent: backend
---

# CI pipeline (build, test, lint)

GitHub Actions workflow runs on every PR + push to `main`. Caches NuGet, runs build + test + lint, fails on warnings.

## Acceptance criteria

- `.github/workflows/ci.yml` runs: restore, build (`/warnaserror`), test (incl. property tests), `dotnet format --verify-no-changes`.
- Test results published as GitHub-summary artifact.
- NuGet cache keyed on `Directory.Packages.props` hash.
- Branch protection on `main` requires the workflow green.
