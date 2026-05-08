---
title: WebhookSubscription aggregate + management API
iteration: 07
status: todo
labels: [delivery, domain, api]
depends-on: []
agent: backend
---

# WebhookSubscription aggregate

`WebhookSubscription` is an Aggregate Root in the Delivery context. Holds `Url`, `Secret` (hashed at rest, returned plaintext once on create), `Kinds` filter, `IsActive`.

## Acceptance criteria

- `IntegratorId`-scoped; uniqueness on `(IntegratorId, Url)`.
- Kinds filter is a set chosen from a closed catalog: `usage.threshold.crossed`, `invoice.generated`, `invoice.credit-note-issued`, `subscription.plan-transitioned`, `subscription.cancelled`, `customer.created`, `customer.renamed`.
- API: `POST /v1/webhooks`, `GET /v1/webhooks`, `DELETE /v1/webhooks/{id}`, `POST /v1/webhooks/{id}/rotate-secret`.
- `WebhookSecretRotated` Domain Event preserved for audit.
