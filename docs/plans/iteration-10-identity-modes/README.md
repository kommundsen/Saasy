# Iteration 10 — Identity Modes

## Goal

Land the full identity stack as decided in [ADR-0007](../../decisions/ADR-0007-admin-dashboard-identity-and-customer-portal-posture.md):

- **Customer Portal** — replace the placeholder integrator-signed JWT from Iteration 09 with the two-mode model: `IntegratorOwned` (signed JWT embed) and `Federated` (OIDC redirect to the Integrator's IdP). Each Integrator picks the mode at provisioning; switching modes is a controlled migration that invalidates Customer sessions.
- **Admin Dashboard** — ASP.NET Core Identity (cookies for the React+Vite SPA) with per-Integrator OIDC SSO, invite-only provisioning, and MFA enforced for Saasy staff and Owners.

The `SaasyIdp` mode that appeared in earlier drafts is not built — Saasy never hosts Customer credentials.

## Out of scope

- SCIM provisioning for the Admin Dashboard (deferred).
- WebAuthn / passkeys (deferred).
- SAML SSO for the Admin Dashboard (OIDC-only in v1).
- An end-customer-facing self-service Identity-Mode-switching UI in Customer Portal — Integrator chooses, Saasy operators configure.

## Exit criteria

- [ADR-0007](../../decisions/ADR-0007-admin-dashboard-identity-and-customer-portal-posture.md) written and accepted.
- `Integrator.IdentityMode = IntegratorOwned | Federated` set at provisioning; mode-specific config (`JwksUrl` or `OidcIssuer + ClientId`) carried as a discriminated value.
- `IntegratorOwned`: Integrator signs JWTs with a key registered with Saasy (RS256/JWKS preferred; HS256 supported for parity with Iteration 09).
- `Federated`: OIDC trust between Saasy and Integrator's IdP; Customer-side SSO redirect flow; per-Integrator OIDC handler resolution (shared machinery with Admin Dashboard SSO).
- Per-Integrator mode change requires Customer re-auth; published as `customer.session-invalidated` (rate-limited).
- Admin Dashboard ships ASP.NET Core Identity scaffold: `AdminUser` + `IntegratorMembership` in an `identity` Postgres schema, cookie auth, invitation flow, MFA enrollment (TOTP), per-Integrator OIDC SSO via `IntegratorOidc` scheme + `PostConfigureOptions<OpenIdConnectOptions>`.
- `is_saasy_staff=true` accounts and `Owner`-role memberships are MFA-required at the framework level; cannot be disabled by the user.
- Saasy-staff impersonation primitive: time-boxed, audit-logged, surfaces in target Integrator's audit log too.
- Both Customer Portal modes resolve to the same `customer_id` claim populated into request context.
- All auth state changes (login, logout, MFA-enroll, OIDC-link, mode-switch, impersonation start/end) recorded in audit log.

## Issues

- [x] [00 — ADR-0007: Admin Dashboard identity & Customer Portal posture](00-adr-0007-admin-identity-and-portal-posture.md)
- [ ] [01 — IdentityMode field on Integrator + provisioning UX](01-identity-mode-on-integrator.md)
- [ ] [02 — `integrator-owned` mode (JWKS, RS256)](02-integrator-owned-mode.md)
- [ ] [04 — `federated` (OIDC) mode integration](04-federated-oidc-mode.md)
- [ ] [05 — Mode-switch migration flow](05-mode-switch-migration.md)

> A separate workstream covers the Admin Dashboard identity scaffold (ASP.NET Core Identity, login UI, MFA, invitations, per-Integrator OIDC SSO). The earliest piece — local accounts + cookie auth for the dashboard — sized inside iteration 04 so the Iter-04 read-only views aren't open to the world. Invitations, MFA enforcement, and per-Integrator OIDC SSO complete in this iteration.
