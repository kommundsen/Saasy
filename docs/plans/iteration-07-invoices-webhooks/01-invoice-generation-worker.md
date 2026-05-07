---
title: Invoice generation worker (FinalClose-triggered)
iteration: 07
status: todo
labels: [invoicing, worker]
depends-on: [00-invoice-lineitem]
---

# Invoice generation worker

A BackgroundService listens for `period.final-closed` from `metering.events`. For each event, generates the Invoice for that Subscription/Period using the FinalClosed Rollups + the PlanVersion in effect during the Period (handles Plan Transitions: split LineItems per sub-period).

## Acceptance criteria

- One Invoice per `(SubscriptionId, Period)` (idempotent via deterministic id).
- LineItems generated per Pricing Component:
  - FlatFee → 1 LineItem per Period (or per sub-period under Proration).
  - PerSeatFee → 1 LineItem with quantity = time-weighted seats.
  - MeteredCharge → 1 LineItem (within-quota = 0 amount, may be omitted; overage as separate LineItem with Description distinguishing).
- `invoice.generated` Domain Event published via Invoicing Outbox.
- Replay on the same `period.final-closed` produces no diff.
