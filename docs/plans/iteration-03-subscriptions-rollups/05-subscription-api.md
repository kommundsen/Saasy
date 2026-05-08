---
title: Subscription API surface
iteration: 03
status: todo
labels: [subscriptions, api]
depends-on: [00-subscription-aggregate]
agent: backend
---

# Subscription API surface

## Acceptance criteria

- `POST /v1/subscriptions` — body: `{ customerId, planId, planVersionId, startAt }`. Returns `Draft` subscription.
- `POST /v1/subscriptions/{id}/activate`, `/pause`, `/resume`, `/cancel`.
- `GET /v1/subscriptions/{id}` returns subscription + current Rollups.
- Returns HTTP 425 Too Early if a required projection is missing (eventual-consistency lag).
- ApiKey auth scopes to `IntegratorId`; cross-Integrator access returns 404.
