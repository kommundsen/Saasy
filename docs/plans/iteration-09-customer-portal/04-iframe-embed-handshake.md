---
title: iframe embed handshake + CSP
iteration: 09
status: todo
labels: [frontend, customer-portal, security]
depends-on: [00-portal-scaffold]
agent: frontend
---

# iframe embed handshake + CSP

The embed target is loaded inside the Integrator's site as an iframe. A `postMessage` handshake exchanges initial state (token, customer id, theme overrides). CSP `frame-ancestors` is per-Integrator allowlist.

## Acceptance criteria

- Bootstrap script publishes `Saasy.embed({ token, integratorId, customerId, target })`.
- iframe origin = portal embed URL; parent origin checked against per-Integrator allowlist.
- `postMessage` envelope schema documented; resize messages supported.
- Integration test in a host harness (Playwright) verifies the handshake.
- Misconfigured `frame-ancestors` blocks loading; helpful console error printed.

> **Note:** mixed-stack issue. Implementing the dominant frontend side first; the backend CSP `frame-ancestors` allowlist side is captured in TBD.
