---
title: federated (OIDC) mode integration
iteration: 10
status: todo
labels: [identity]
depends-on: [01-identity-mode-on-integrator]
---

# `federated` (OIDC) mode

OIDC trust between Saasy and the Integrator's IdP. Customer Portal redirects to Integrator IdP; on return, Saasy validates the ID token + issues a portal session.

## Acceptance criteria

- Per-Integrator OIDC config: `Issuer`, `ClientId`, `ClientSecret` (encrypted), `RedirectUri`, `Scopes`.
- Authorization-code flow with PKCE.
- Mapping rule for `sub` → `customer_id` (configurable: `external_ref` lookup or claim mapping).
- Logout propagation supported (front-channel logout).
- Documented runbook for IdP credential rotation.
