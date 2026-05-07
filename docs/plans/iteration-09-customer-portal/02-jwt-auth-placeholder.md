---
title: Integrator-signed JWT auth (placeholder identity mode)
iteration: 09
status: todo
labels: [identity, customer-portal, security]
depends-on: []
---

# Integrator-signed JWT auth

Placeholder of the `integrator-owned` Identity Mode (full identity work in Iteration 10). Integrator mints a short-lived JWT (HS256 with the per-Integrator secret) carrying `customer_id` and `aud=saasy-portal`; portal calls Saasy Api with this token.

## Acceptance criteria

- Saasy Api validates HS256 signature, audience, expiry; populates `customer_id` claim into request context.
- Token TTL ≤ 15 minutes; clock-skew tolerance 60s.
- Portal includes a refresh hook that calls back into the Integrator's site for a new token.
- Failure modes (expired, bad signature, wrong aud) return 401 with WWW-Authenticate hint.
- This issue is replaced by Iteration 10's full identity system; treat it as scaffolding.
