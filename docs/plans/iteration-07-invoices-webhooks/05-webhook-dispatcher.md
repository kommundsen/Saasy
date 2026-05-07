---
title: Webhook Dispatcher (signed, retried, DLQ)
iteration: 07
status: todo
labels: [delivery, worker]
depends-on: [04-webhook-subscription]
---

# Webhook Dispatcher

A BackgroundService consumes the bus topics and fans out HTTP POSTs to matching `WebhookSubscription`s. Bodies are HMAC-SHA256 signed with the subscription's secret; signature in `X-Saasy-Signature`.

## Acceptance criteria

- Per-`WebhookSubscription` Outbox + Dispatcher (Delivery context owns this).
- Retries: exponential backoff at 1s, 5s, 30s, 5m, 30m, 2h, 24h. After last failure, dead-letter.
- DLQ inspection endpoint `GET /v1/webhooks/{id}/dead-letters`.
- Headers: `X-Saasy-Event-Kind`, `X-Saasy-Event-Id`, `X-Saasy-Delivery-Id`, `X-Saasy-Signature`, `X-Saasy-Signature-Version: v1`.
- Idempotent on retry: receiver can safely de-dupe by `X-Saasy-Event-Id`.
- Telemetry: success/failure counters, retry histograms.

## References

- [ADR-0008](../../decisions/ADR-0008-internal-domain-event-bus.md)
