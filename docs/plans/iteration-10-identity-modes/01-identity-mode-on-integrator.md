---
title: IdentityMode field on Integrator + provisioning UX
iteration: 10
status: todo
labels: [tenancy, identity, domain]
depends-on: []
agent: backend
---

# IdentityMode field on Integrator

`Integrator.IdentityMode: IntegratorOwned | Federated` set at creation; switch is a controlled migration (issue 05). Per [ADR-0007](../../decisions/ADR-0007-admin-dashboard-identity-and-customer-portal-posture.md) the `SaasyIdp` mode does not exist — Saasy never hosts Customer credentials.

## Acceptance criteria

- Strongly-Typed enum on `Integrator` (`IntegratorOwned | Federated`).
- API exposes the field on Integrator create + read.
- Mode-specific config carried as a discriminated value (`JwksUrl` for `IntegratorOwned`, `OidcIssuer + ClientId + scopes` for `Federated`; client secret resolved from Key Vault by Integrator slug).
- Validation prevents inconsistent config (e.g., setting JwksUrl on Federated mode).
