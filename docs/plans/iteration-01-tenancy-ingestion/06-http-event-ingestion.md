---
title: HTTP POST /v1/events ingestion endpoint
iteration: 01
status: todo
labels: [ingestion, api]
depends-on: [03-apikey-auth-middleware, 05-event-ingestion-consumer]
agent: backend
---

# HTTP POST /v1/events

Fallback ingestion path for low-volume Integrators. Same envelope shape as Event Hub. Writes either directly to the raw store **or** publishes to the Event Hub depending on configured strategy.

## Acceptance criteria

- `POST /v1/events` accepts a single event or a batch (≤ 500 per request).
- 202 Accepted on success; 400 on shape errors with field-level detail.
- Per-Integrator request volume cap returns 429 (cap value bound to Tier).
- Same idempotency semantics as the Hub path.
- OpenAPI spec generated.
