---
title: Observability sink decision (ADR)
iteration: 04
status: todo
labels: [observability, adr]
depends-on: []
agent: human
---

# Observability sink decision

Decide between Azure Monitor (Application Insights), Grafana Cloud + Prometheus + Tempo, or self-hosted OTel collector + Jaeger + Prometheus. Record the decision so the Admin Dashboard support flow has a known operational surface.

## Acceptance criteria

- ADR written under `docs/decisions/` (next available number).
- Covers traces, metrics, logs, retention windows, cost envelope, on-call runbook implications.
- Decision is wired into `Saasy.Api` and `Saasy.Worker` OTel exporters.
- Crossed off in [iterations.md](../iterations.md) "Open decisions".
