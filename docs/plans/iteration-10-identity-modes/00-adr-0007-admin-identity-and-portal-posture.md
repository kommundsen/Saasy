---
title: ADR-0007 — Admin Dashboard identity & Customer Portal posture
iteration: 10
status: done
labels: [identity, adr]
depends-on: []
agent: human
---

# ADR-0007 — Admin Dashboard identity & Customer Portal posture

Decision recorded at [docs/decisions/ADR-0007](../../decisions/ADR-0007-admin-dashboard-identity-and-customer-portal-posture.md). Reframed during planning: the original "pick a hosted IdP for `saasy-idp` mode" question conflated two unrelated populations. The settled split:

- **Customer Portal** — Integrator-controlled identity only. Two modes: `integrator-owned` (signed JWT embed) and `federated` (OIDC redirect). The `saasy-idp` mode is removed; Saasy never hosts Customer credentials.
- **Admin Dashboard** — ASP.NET Core Identity (Postgres-backed, EF Core), cookie auth for the React+Vite SPA, per-Integrator OIDC SSO via dynamic handler resolution. New `AdminUser` + `IntegratorMembership` schema in an `identity` Postgres schema.

## Acceptance criteria (met at ADR acceptance)

- ADR `docs/decisions/ADR-0007-admin-dashboard-identity-and-customer-portal-posture.md` written and accepted.
- `saasy-idp` mode and the corresponding Iter-10 issue removed from the plan.
- Open decisions checklist in [iterations.md](../iterations.md) updated.

## Downstream issues

The remaining iteration-10 issues are scoped against the two-mode model:

- [01 — IdentityMode field on Integrator + provisioning UX](01-identity-mode-on-integrator.md) — enum becomes `IntegratorOwned | Federated`.
- [02 — `integrator-owned` mode (JWKS, RS256)](02-integrator-owned-mode.md) — unchanged scope.
- [04 — `federated` (OIDC) mode integration](04-federated-oidc-mode.md) — unchanged scope; same per-Integrator OIDC machinery as Admin Dashboard SSO, applied to the Customer population.
- [05 — Mode-switch migration flow](05-mode-switch-migration.md) — only one switch direction matters now (`IntegratorOwned` ↔ `Federated`).

A separate workstream (sized inside iter-04 for the auth scaffold, completed inside iter-10 for invitations + OIDC SSO) covers the Admin Dashboard identity stack: ASP.NET Core Identity scaffold, AdminUser/IntegratorMembership migration, login UI, OIDC SSO redirect handler, MFA enrollment, invitation flow.
