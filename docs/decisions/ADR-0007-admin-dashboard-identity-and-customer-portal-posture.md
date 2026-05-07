---
id: ADR-0007
type: adr
state: accepted
title: Admin Dashboard identity + Customer Portal posture
date: 2026-05-08
deciders: [kim]
---

# ADR-0007 — Admin Dashboard identity & Customer Portal posture

## Context

The Iteration 10 plan listed three modes for "who authenticates a Customer Portal session": `integrator-owned`, `saasy-idp`, and `federated`. The original ADR-0007 framing was "pick a hosted IdP for `saasy-idp` mode" (Auth0, Entra External ID, Keycloak).

That framing conflates two distinct populations whose identity needs are unrelated:

- **Admin Dashboard users** — Integrator engineers / RevOps who log into Saasy to configure Product Types, Plans, Subscriptions, and inspect the system. Plus Saasy's own Ops Console operators (per [ADR-0014](ADR-0014-cross-context-write-and-consistency-model.md)). Bounded population — tens to low hundreds per Integrator.
- **Customer Portal users** — the Integrator's *end-customers*, viewing their own Subscription, usage, and Invoices. Unbounded B2B2C population — scales with Integrator success.

Hosting identity for the Customer Portal puts Saasy in the B2B2C identity business: per-Integrator tenant/realm isolation, theming pass-through, MAU pricing exposure, and GDPR liability for credentials Saasy never needed. Hosting identity for the *Admin Dashboard* is a small, well-understood B2B problem already solved by the framework Saasy is built on.

This ADR splits the two cleanly, drops the `saasy-idp` mode entirely, and settles the Admin Dashboard identity stack.

## Decision

### Customer Portal — Integrator-controlled identity only

The Customer Portal supports **two** identity modes, neither of which requires Saasy to host Customer credentials:

- **`integrator-owned`** — the Integrator's backend signs a short-lived JWT and embeds the Customer Portal as an iframe (or hands the JWT to a redirected SPA load). Saasy validates the JWT against a per-Integrator JWKS (RS256 preferred, HS256 supported for parity with the Iter-09 placeholder). The signed JWT is the Integrator's session.
- **`federated`** — Saasy initiates an OIDC redirect to the Integrator's IdP; on return, the OIDC `sub` claim resolves to a Saasy `customer_id` via a per-Integrator mapping table.

The previously-listed **`saasy-idp` mode is removed.** Saasy never hosts Customer credentials, never operates a B2B2C IdP, and never enters the per-MAU pricing curve that pattern implies.

[ADR-0009](ADR-0009-integrator-per-kind-environment-model.md) already commits to per-Integrator isolation at the data layer; pushing identity to the Integrator side keeps that boundary clean.

### Admin Dashboard — ASP.NET Core Identity, cookie-based, per-Integrator OIDC SSO

The Admin Dashboard authenticates Integrator employees and Saasy staff. The stack:

- **ASP.NET Core Identity** ([learn.microsoft.com/aspnet/core/security/authentication/identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity?view=aspnetcore-10.0)) for local accounts. EF Core schema in the same Postgres database as the rest of Saasy ([ADR-0003](ADR-0003-primary-database.md)), in a dedicated `identity` schema. Default password hashing (PBKDF2 with HMAC-SHA512), authenticator-app TOTP MFA, account lockout, security stamp.
- **Cookie authentication** for the Admin Dashboard SPA (React + Vite, [ADR-0004](ADR-0004-frontend-stack.md)). The SPA is served same-site to the API, so the auth cookie is HttpOnly, Secure, SameSite=Lax. No bearer tokens for the SPA — the framework's `IdentityConstants.ApplicationScheme` cookie is the documented recommendation for browser-based apps.
- **Per-Integrator OIDC SSO** as a parallel scheme. An Integrator employee logging in can either (a) use a local Identity account or (b) click "Sign in with `<their company>`" which redirects to their corporate OIDC provider; the OIDC `email` claim resolves to a local Identity user, who must already be invited and provisioned (no JIT creation in v1).
- **Identity bearer-token endpoints (`AddBearerToken(IdentityConstants.BearerScheme)`) are not exposed.** The SPA uses cookies; CLI and server-to-server clients use the ApiKey aggregate from Iter-01. Identity's bearer tokens are documented as "intentionally simple, not a full IdP" and aren't a fit for either population Saasy serves.

### Schema — AdminUser and IntegratorMembership

A new aggregate, deliberately separate from Customer:

```
AdminUser  (extends IdentityUser<Guid>)
  - email, password_hash, security_stamp, lockout_*, two_factor_*  (Identity defaults)
  - is_saasy_staff: bool      <-- gates Ops Console access
  - external_oidc_subjects: list of (integrator_id, oidc_issuer, oidc_subject)
                              <-- populated when user signs in via Integrator OIDC

IntegratorMembership
  - user_id (-> AdminUser)
  - integrator_id (-> Integrator)
  - role: enum { owner, admin, viewer }
  - invited_at, accepted_at
```

A user belongs to many Integrators (Saasy staff to all; consultants to several; most users to one). `is_saasy_staff` is the single flag that grants Ops Console access; it is set only by other Saasy staff and audit-logged.

`AdminUser` is **not** an Aggregate Root in the DDD sense — it's a framework-managed identity record. Saasy's domain model treats it as an external entity whose only domain-relevant projection is `IntegratorMembership`. This is the same separation [ADR-0014](ADR-0014-cross-context-write-and-consistency-model.md) draws between cross-context-bound entities and aggregate-internal ones.

### Per-Integrator OIDC — dynamic handler resolution

A static `AddOpenIdConnect(scheme, ...)` per Integrator does not scale and cannot be reconfigured at runtime. The pattern instead:

- Single registered authentication scheme `IntegratorOidc`.
- A `PostConfigureOptions<OpenIdConnectOptions>` resolves the per-Integrator configuration (issuer URL, client ID, client secret, scopes) from a DB table `IntegratorOidcProvider`, keyed by an `integrator` query parameter on the challenge URL (`/auth/sso?integrator=acme-co`).
- Client secrets live in Azure Key Vault, looked up by Integrator slug. Rotation is a Key Vault concern, not an application deploy.
- `IAuthenticationSchemeProvider` is overridden to surface the resolved scheme to the middleware. This is a known multi-tenant pattern; reference implementations exist (Tomas Trajan's "Multi-tenant OpenID Connect" pattern, the IdentityServer multi-tenant samples).

The application code is generic; configuration is data.

### Account provisioning — invite-only in v1

- New `AdminUser` rows are created exclusively via an invitation flow: an existing Owner/Admin invites by email, an email link grants the recipient the option to set a local password OR sign in via the configured OIDC provider for that Integrator.
- **No JIT account creation from OIDC.** A user signing in via OIDC must already be invited and have an `IntegratorMembership` row. This prevents an OIDC misconfiguration from silently provisioning unintended access.
- Saasy staff onboarding follows the same flow with `is_saasy_staff=true` set at invite time.
- Invitation links are 7-day single-use, hashed at rest, audit-logged on send/accept/expiry.

### MFA

- **Required for `is_saasy_staff=true` accounts.** Cannot be disabled.
- **Required for `role=owner` IntegratorMembership.** Cannot be disabled by the user; only by another Owner removing the role.
- **Optional for `admin` and `viewer` roles** — the Integrator's Owner can mandate it organisation-wide via a policy flag on Integrator. The flag's existence is part of this ADR; the UI to flip it is iter-10 work.
- TOTP via authenticator app is the only second factor in v1. SMS is not offered. WebAuthn / passkeys are deferred to a future ADR.

### Saasy staff (Ops Console) auth

The Ops Console (Saasy-internal, per the design system) uses the same Identity store, gated by `is_saasy_staff=true`. There is no separate identity stack for Saasy operators.

- Saasy staff accounts are MFA-required (above).
- Saasy staff *may* have IntegratorMembership rows for impersonation / support access; impersonation is an explicit, time-boxed, audit-logged action, not a side effect of signing in. The impersonation primitive is an iter-10 deliverable.

## Consequences

### Wins

- **Saasy is out of the B2B2C identity business.** No Auth0 bill, no Keycloak realms, no GDPR exposure on Customer credentials, no per-MAU cost curve.
- **Admin Dashboard auth uses framework-default machinery.** ASP.NET Core Identity + cookies is the documented recommendation for SPAs hosted same-site to their API. Aspire integration is built in.
- **Per-Integrator OIDC SSO unblocks enterprise sales** without committing Saasy to a specific corporate IdP — Integrators bring their own (Entra ID, Okta, Google Workspace, anything OIDC-spec-compliant).
- **Single Postgres database, single backup story.** Identity rows live next to domain rows; no cross-store consistency to manage.
- **Customer Portal modes simplify to two.** The `IdentityMode` enum on Integrator becomes binary; Iteration 10 issue 03 (`saasy-idp` integration) disappears entirely.
- **MFA is enforceable on the populations that matter** (Saasy staff, Owners) without forcing it on every Integrator employee.

### Costs

- **Per-Integrator dynamic OIDC resolution is non-trivial.** The `PostConfigureOptions` + `IAuthenticationSchemeProvider` pattern is well-documented but adds custom auth middleware that must be tested under multi-Integrator concurrency. Bug surface is real.
- **Saasy operates a password-storage surface.** PBKDF2 hashing, lockout, security-stamp invalidation are framework-default but still a thing to monitor for credential-stuffing patterns. Application Insights ([ADR-0018](ADR-0018-observability-sinks.md)) is the alerting path.
- **Integrators without an OIDC provider use local Identity passwords**, which is a worse experience than corporate SSO. v1 ships with both; the upgrade path is "configure your OIDC provider in Settings."
- **Invite-only provisioning slows enterprise rollouts.** Customers expect to bulk-import their team via SCIM. SCIM provisioning is deferred to a future ADR.
- **The `integrator-owned` JWT-embed mode is the lower-friction Customer Portal option but requires Integrator backend work** to mint signed JWTs. The `federated` mode is more setup once but lower per-request work; both are supported because Integrator preferences vary.
- **Customer Portal `federated` mode requires per-Integrator OIDC config** identical in shape to the Admin Dashboard's, but applied to a different population. The implementation can share code; the configuration is separate.

### Out of scope

- **SCIM provisioning** for Admin Dashboard accounts. Deferred.
- **WebAuthn / passkeys** as a second factor. Deferred to a future ADR.
- **SAML SSO** for the Admin Dashboard. OIDC-only in v1; SAML-only enterprise IdPs are rare enough that the workaround (use Entra ID's OIDC bridge, etc.) is acceptable.
- **Customer-managed encryption keys (CMK)** for the Identity store at rest. Microsoft-managed keys, mirroring [ADR-0017](ADR-0017-outbox-retention-and-cold-archive.md) and [ADR-0018](ADR-0018-observability-sinks.md).
- **Cross-Integrator user accounts as a first-class feature.** A user can have IntegratorMembership rows in many Integrators, but there is no "switch Integrator" UX optimised for managing dozens. Targeted at consultants and Saasy staff; if Integrators grow accounts that span tens of Integrators the UX is revisited.
- **Snapshot / replay of Identity events into a domain projection.** Identity is intentionally outside the [ADR-0014](ADR-0014-cross-context-write-and-consistency-model.md) cross-context model — auth state isn't a domain projection, it's a session concern.
