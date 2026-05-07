---
title: OpenTelemetry baseline
iteration: 00
status: todo
labels: [foundations, observability]
depends-on: [02-aspire-apphost]
---

# OpenTelemetry baseline

Wire OpenTelemetry traces + metrics + logs in Api and Worker. Aspire dev dashboard receives them locally; production sink decision deferred (see open ADRs in [iterations.md](../iterations.md)).

## Acceptance criteria

- `OpenTelemetry.Extensions.Hosting` configured in both Api and Worker.
- Traces emitted for incoming HTTP requests and Worker BackgroundService cycles.
- Resource attributes include `service.name`, `service.namespace=saasy`, `deployment.environment`.
- Aspire dev dashboard shows traces end-to-end.

## References

- [ADR-0002](../../decisions/ADR-0002-backend-stack.md)
