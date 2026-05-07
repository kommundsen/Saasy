---
title: Mode-switch migration flow
iteration: 10
status: todo
labels: [identity, operations]
depends-on: [02-integrator-owned-mode, 04-federated-oidc-mode]
---

# Mode-switch migration flow

Switching IdentityMode is rare but supported. Existing Customer auth state is invalidated; Customers must re-authenticate via the new mode.

## Acceptance criteria

- Operator-only API: `POST /ops/integrators/{id}/identity-mode` with new mode + config.
- Switch records an audit event with old/new modes + operator id.
- All active sessions for that Integrator's Customers expire immediately.
- Outbound `customer.session-invalidated` Domain Event published per Customer (rate-limited).
- Documented playbook in `docs/runbooks/identity-mode-switch.md`.
