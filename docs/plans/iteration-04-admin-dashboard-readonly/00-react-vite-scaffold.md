---
title: React + Vite scaffold + Aspire integration
iteration: 04
status: todo
labels: [frontend, admin-dashboard, aspire]
depends-on: []
---

# React + Vite scaffold + Aspire integration

`apps/admin-dashboard` is a Vite-built SPA. Aspire serves it via a static endpoint in dev and proxies API calls to `Saasy.Api`.

## Acceptance criteria

- pnpm workspace at repo root with `apps/` and `packages/` directories.
- `apps/admin-dashboard` runs via `pnpm dev` and via Aspire AppHost.
- Routing via React Router; root route lists Integrators.
- Build output served as static files; CSP-friendly.
- No domain logic in the SPA — only API calls.

## References

- [ADR-0004](../../decisions/ADR-0004-frontend-stack.md)
