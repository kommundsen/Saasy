---
title: Service Bus topics + sessioned consumers
iteration: 03
status: todo
labels: [infrastructure, eventing, aspire]
depends-on: []
agent: backend
---

# Service Bus topology

Provision Service Bus namespace with one topic per emitting context. Subscriptions on each topic are session-enabled (`integrator_id`) so single-Integrator ordering is preserved.

## Acceptance criteria

- Aspire AppHost runs Service Bus emulator locally.
- Topics: `tenancy.events`, `catalog.events`, `subscriptions.events`, `metering.events`.
- Subscriptions on each topic are session-aware.
- Duplicate detection window: 10 minutes.
- Bicep / Terraform module mirrors emulator topology.

## References

- [ADR-0008](../../decisions/ADR-0008-internal-domain-event-bus.md)
