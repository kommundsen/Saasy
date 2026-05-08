---
title: ApiKey lifecycle (hashed storage, revocation)
iteration: 01
status: todo
labels: [tenancy, domain, security]
depends-on: [00-integrator-aggregate]
agent: backend
---

# ApiKey lifecycle

`ApiKey` is a Child Entity inside `Integrator`. Mint returns the plaintext once; storage holds only a salted hash + last-4 prefix for display. Revocation is soft (sets `RevokedAt`).

## Acceptance criteria

- `Integrator.MintApiKey(name)` returns `(ApiKey entity, string plaintextSecret)`.
- Hash algorithm: argon2id or PBKDF2 with documented parameters.
- `RevokeApiKey(apiKeyId)` sets `RevokedAt`; revoked keys never authenticate.
- API endpoint `POST /v1/integrators/{id}/api-keys` returns plaintext exactly once.
- Property test: hash is deterministic for same input + salt; differs across salts.
