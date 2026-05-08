---
title: usage.threshold.crossed Domain Event
iteration: 05
status: todo
labels: [metering, eventing]
depends-on: [02-threshold-firing-logic]
agent: backend
---

# `usage.threshold.crossed` Domain Event

Domain Event payload emitted to `metering.events`. Webhook fan-out happens in Iteration 07; this iteration only ensures the event is on the bus with a stable contract.

## Acceptance criteria

- Schema versioned (`v1`):
  - `integrator_id`, `customer_id`, `subscription_id`, `dimension_id`, `period_start`, `period_end`, `percent`, `aggregated_value`, `quota`, `crossed_at`.
- Published via Metering Outbox.
- Contract recorded under `docs/architecture/event-contracts.md` (create if absent).
- Bus consumer test verifies the message is delivered with session = `integrator_id`.
