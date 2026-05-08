---
title: ApiKey authentication middleware
iteration: 01
status: todo
labels: [tenancy, security, api]
depends-on: [01-apikey-lifecycle]
agent: backend
---

# ApiKey authentication middleware

ASP.NET Core authentication scheme that reads `Authorization: ApiKey <secret>`, hashes, looks up by prefix, validates, and binds `IntegratorId` into the request context for downstream scoping.

## Acceptance criteria

- Custom auth handler registered as `ApiKey` scheme; default scheme on `/v1/*` endpoints.
- Failed lookups return 401 with no body; logging records prefix + reason.
- Successful auth populates `HttpContext.User` claims with `integrator_id`, `integrator_kind`, `integrator_tier`.
- Constant-time hash comparison to mitigate timing attacks.
- Integration test covers valid, missing, malformed, and revoked keys.
