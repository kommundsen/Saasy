# Iteration 07 — Invoices & Webhooks

## Goal

Generate deterministic Invoices at `FinalClose` and deliver outbound Webhooks. Invoice output is JSON + HTML (no PDF). Webhook delivery uses Delivery context Outbox, retry with backoff, signed payloads, per-`WebhookSubscription` filtering. After this iteration, the v1 metering+billing+notification loop is end-to-end.

## Out of scope

No PDF rendering. No payment movement. No tiered pricing yet (Iteration 08).

## Exit criteria

- `Invoice` aggregate generated automatically at FinalClose; one `Invoice` per `(SubscriptionId, Period)`.
- LineItems computed from Rollups + PricingComponents; deterministic ordering; total in Plan currency.
- `CreditNote` aggregate supports issuing corrections against a previously issued Invoice.
- JSON + HTML rendering reproducible byte-for-byte from the same Invoice state.
- `WebhookSubscription` aggregate in Delivery context: URL, secret, kinds filter, channel routing.
- Webhook delivery: HMAC signed body, retries with exponential backoff, dead-letter after N attempts.
- Domain Events from earlier iterations fan out: `usage.threshold.crossed`, `invoice.generated`, `subscription.plan-transitioned`, `subscription.cancelled`, `customer.created`, `customer.renamed`.
- Property tests for invoice determinism on identical Rollup snapshots.
- Production deployment topology decision (open ADR) recorded.

## Issues

- [ ] [00 — Invoice + LineItem aggregates](00-invoice-lineitem.md)
- [ ] [01 — Invoice generation worker (FinalClose-triggered)](01-invoice-generation-worker.md)
- [ ] [02 — JSON + HTML renderers (deterministic)](02-invoice-renderers.md)
- [ ] [03 — CreditNote aggregate + correction flow](03-credit-note.md)
- [ ] [04 — WebhookSubscription aggregate + management API](04-webhook-subscription.md)
- [ ] [05 — Webhook Dispatcher (signed, retried, DLQ)](05-webhook-dispatcher.md)
- [ ] [06 — Property tests for invoice determinism](06-pbt-invoice-determinism.md)
- [ ] [07 — Deployment topology ADR + first prod deploy](07-deployment-topology-adr.md)
