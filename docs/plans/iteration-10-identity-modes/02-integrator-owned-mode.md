---
title: integrator-owned mode (JWKS, RS256)
iteration: 10
status: todo
labels: [identity, security]
depends-on: [01-identity-mode-on-integrator]
---

# `integrator-owned` mode

Integrator signs JWTs; Saasy validates against a `JwksUrl` (rotated keys supported). Replaces the HS256 placeholder from Iteration 09.

## Acceptance criteria

- JWKS fetched + cached with respect to `Cache-Control` headers; min refresh interval 5 minutes, max 24 hours.
- Algorithms allowed: RS256, ES256.
- Required claims: `iss` (matches Integrator config), `aud=saasy`, `customer_id`, `exp`, `iat`.
- Clock skew 60s.
- Failure cases tested: expired key, mismatched issuer, wrong aud, missing customer_id.
