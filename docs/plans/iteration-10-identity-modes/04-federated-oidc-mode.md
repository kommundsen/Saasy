---
title: federated (OIDC) mode integration
iteration: 10
status: todo
labels: [identity]
depends-on: [01-identity-mode-on-integrator]
agent: backend
---

# `federated` (OIDC) mode

OIDC trust between Saasy and the Integrator's IdP. Customer Portal redirects to Integrator IdP; on return, Saasy validates the ID token + issues a portal session.

## Acceptance criteria

- Per-Integrator OIDC config: `Issuer`, `ClientId`, `ClientSecret` (encrypted), `RedirectUri`, `Scopes`.
- Authorization-code flow with PKCE.
- Mapping rule for `sub` → `customer_id` (configurable: `external_ref` lookup or claim mapping).
- Logout propagation supported (front-channel logout).
- Documented runbook for IdP credential rotation.

> **Note:** mixed-stack issue. Implementing the dominant backend side first; the frontend redirect/return UI side is captured in TBD (small slice in iter-04 / iter-09).
