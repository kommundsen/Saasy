---
id: ADR-0018
type: adr
state: accepted
title: Observability sinks
date: 2026-05-08
deciders: [kim]
---

# ADR-0018 — Observability sinks

## Context

Multiple ADRs have stamped concrete observability requirements:

- [ADR-0014](ADR-0014-cross-context-write-and-consistency-model.md) §projection-lag SLO mandates outbox-dispatch-lag and projection-consume-lag histograms, per-projection high-water and last-advance-timestamp gauges, and an alert path when p99 > 30s sustained for 5 minutes or no advance for 60s.
- [ADR-0012](ADR-0012-threshold-firing-rules.md) requires threshold-firing latency be measurable end-to-end (Event commit → `usage.threshold.crossed` emission).
- [ADR-0010](ADR-0010-period-boundaries-and-final-close.md) requires Final Close timing be observable per-Integrator.
- [ADR-0008](ADR-0008-internal-domain-event-bus.md) requires Service Bus session keying and per-Integrator FIFO be verifiable from telemetry.

[ADR-0002](ADR-0002-backend-stack.md) commits to .NET + Aspire, which emits OpenTelemetry Protocol (OTLP) natively. The Iteration 04 Admin Dashboard ships against these SLOs, so an observability backend is a hard prerequisite for that iteration.

The category fork is *Azure-native (Application Insights / Azure Monitor) vs. vendor-neutral (Grafana stack, Honeycomb, Datadog)*. This ADR settles it as **Azure-native**, with an OpenTelemetry Collector in the path so the backend choice is config-replaceable.

## Decision

### Pipeline shape — OTel Collector in front of Azure Monitor

Every Saasy service (Api, Worker, future web BFFs) emits OTLP to a side-car or cluster-local **OpenTelemetry Collector**. The Collector exports to **Azure Monitor / Application Insights** via the Azure Monitor exporter.

```
[ Service ] --OTLP--> [ OTel Collector ] --AzMon Exporter--> [ Application Insights ]
                              |
                              +--(future: secondary exporter)--> [ alt backend ]
```

- The Collector is non-negotiable; direct App-Insights-SDK use in services is forbidden. This keeps the swap-the-backend escape hatch real: a backend change is a Collector-config change, not a fleet redeploy.
- In local development, **Aspire's dashboard** consumes OTLP directly. The Collector is only present in deployed environments.
- The Collector runs as a sidecar (one per service instance) in deployed environments. Cluster-shared collectors are deferred until service count makes per-instance overhead measurable.

### Backend — Application Insights, one resource per service per environment

- **One Log Analytics Workspace per environment** (`saasy-obs-sandbox`, `saasy-obs-prod`). All Application Insights resources point at the matching workspace; cross-service correlation uses workspace-scoped Kusto queries.
- **One Application Insights resource per service per environment** (`appi-api-prod`, `appi-worker-prod`, etc.). Per-service resources keep ingestion caps and access controls scoped.
- **No separate metrics store.** Application Insights metrics + Log Analytics metrics tables are the single sink. Azure Monitor managed Prometheus is not used for Saasy v1 — adds operational surface without changing what's queryable.
- **Logs, traces, and metrics share the workspace.** Correlation by `operation_id` / `parent_id` is built into App Insights; reproducing it on a split-stack would be effort for nothing.

### Instrumentation surface — what every service emits

Mandatory across all .NET services:

- **Distributed traces.** ASP.NET Core, HttpClient, Service Bus client, Event Hubs client, Npgsql, EF Core — all auto-instrumented via the Aspire-default OTel packages. Custom spans on every domain-method invocation in the application layer (one span per command handler, one per event handler).
- **Metrics — the [ADR-0014](ADR-0014-cross-context-write-and-consistency-model.md) §projection-lag set, mandatory per consuming context:**
  - `saasy.outbox.dispatch.lag` (histogram, ms) — relay segment, tagged `(producing_context)`.
  - `saasy.projection.consume.lag` (histogram, ms) — consumer segment, tagged `(producing_context, consuming_context, projection_name)`.
  - `saasy.projection.high_water` (gauge, version) — drives the 425-vs-404 decision.
  - `saasy.projection.last_advance_timestamp` (gauge, unix-ms) — drives stuck-detection.
- **Metrics — domain-specific:**
  - `saasy.threshold.fire.latency` (histogram, ms) — Event commit → `usage.threshold.crossed` emission, tagged `(integrator_id, dimension)`.
  - `saasy.event.ingest.lag` (histogram, ms) — Event Hub enqueue → raw Event landed in Postgres.
  - `saasy.period.close.duration` (histogram, ms) — Final Close start → completion, tagged `(integrator_id)`.
- **Logs — structured, correlation-stamped.** `Microsoft.Extensions.Logging` with the OTel logging provider. Every log carries the active trace context (`trace_id`, `span_id`); manual `operation_id` propagation is forbidden — let the SDK do it.
- **Cross-service correlation through Service Bus and Event Hubs** uses W3C trace-context (`traceparent` / `tracestate`) in the message envelope. The envelope shape from [ADR-0008](ADR-0008-internal-domain-event-bus.md) reserves these headers explicitly.

All metric names use the `saasy.` prefix and the OpenTelemetry semantic-conventions naming rules where applicable. Histograms use the OTel default explicit-bucket boundaries; the Aspire defaults are accepted as adequate until production data argues otherwise.

### Sampling

- **Sandbox:** 100% trace sampling, no head- or tail-based reduction. Cost cap (below) is the safety net.
- **Production:** **head-based at 100%** in v1. The projected request volume across Api + Worker doesn't justify tail-based sampling complexity, and full sampling makes the lag-SLO triage queries trivially representative. If ingestion volume drives cost past the cap, the first move is **tail-based sampling at the Collector** (`tailsamplingprocessor`) keyed on: keep all errors, keep all traces touching `period_close` or `final_close`, sample 10% of the rest. Implementing tail sampling is an operational change, not an ADR change.

### Cost controls

- **Daily ingestion cap per Application Insights resource**, set at 2× the rolling 30-day p95 daily volume per resource. Cap-hit triggers a `Sev3` alert; data past the cap is dropped (App Insights default behaviour).
- **Log volume budget** is set by environment, not by service. Budget breaches are reviewed weekly until v1 traffic stabilises.
- **PII filtering happens at the Collector**, not at the SDK call site. A `transformprocessor` strips known PII fields (`email`, `phone`, `address`, anything in a `pii.*` namespace) before export. Application code is allowed to log these (e.g. for local debugging via the Aspire dashboard); the Collector is the only choke point that matters for what reaches App Insights.
- **Customer event payloads are never logged.** A separate Collector rule drops any log record carrying a `saasy.event.payload` attribute; the attribute exists so handlers can attach the payload to a trace span (queryable, sampled with the trace) rather than spilling it into log search.

### Alerting

- **Lag-SLO alerts** ([ADR-0014](ADR-0014-cross-context-write-and-consistency-model.md)) — Azure Monitor alert rules over the `saasy.projection.consume.lag` and `saasy.projection.last_advance_timestamp` metrics. Thresholds copied verbatim from the source ADR.
- **Threshold-firing latency** ([ADR-0012](ADR-0012-threshold-firing-rules.md)) — alert when `saasy.threshold.fire.latency` p99 > 30s for 5 minutes.
- **Ingestion cap proximity** — warn at 80% of daily cap; page at 100% to acknowledge dropped data.
- **Alert routing** — PagerDuty (via Azure Monitor action group → webhook) for `Sev1`/`Sev2`; email/Teams for `Sev3`. PagerDuty service identity is not part of this ADR; it's a deployment-time secret.
- **Alert definitions live in code** — Bicep modules under `infra/observability/`. Alert rules don't get edited in the portal; portal edits are reverted by the next infra deploy.

### Aspire integration

- Aspire's dashboard is the **local-dev** observability surface. Service start-up emits OTLP to the dashboard's collector at `http://localhost:18889`. No App Insights emission in local dev.
- Aspire's `AddServiceDefaults()` extension is the only way services configure OTel. Bespoke per-service `TracerProviderBuilder` configuration is forbidden; if a service needs additional instrumentation, it lands in `ServiceDefaults`.
- The Aspire AppHost project does not deploy the Collector — Collector deployment is the responsibility of the deployment-topology ADR (forthcoming, gates Iter 07). For Iter 04 integration, the local Aspire dashboard suffices.

## Consequences

### Wins

- **One vendor for v1.** Azure Postgres, Service Bus, Event Hubs, Storage, plus App Insights — single bill, single auth model, single quota dashboard.
- **Aspire integration is free.** The default `AddServiceDefaults()` wiring already does what this ADR mandates; per-service config is near-zero.
- **The OTel Collector preserves the swap-out.** A future move to Honeycomb / Tempo / Datadog is a Collector exporter swap, not an application-code change. The cost of keeping the option open is the Collector itself.
- **Lag-SLO and threshold-latency alerts have a concrete path** from emission (Aspire-default OTel) → ingestion (App Insights) → rule (Azure Monitor) → page (PagerDuty).
- **PII filtering at the Collector** centralises the privacy boundary. App code doesn't need to know which fields are sensitive at log time; the Collector enforces it.
- **Cap-then-drop** is a hard cost ceiling. No surprise bills.

### Costs

- **App Insights' Kusto-based queries are less ergonomic than Honeycomb's BubbleUp** for high-cardinality trace exploration. The Iteration 04 dashboards will lean on KQL workbooks; complex root-cause queries take longer to compose. Acceptable at v1 scale.
- **Sidecar-Collector overhead** on every service instance — small (tens of MB RSS per service), but non-zero, and the Collector is one more thing to upgrade.
- **Cap-hit drops data**, which is a correctness hazard during incidents. The cap is set generously (2× p95) and monitored at 80%, but a sustained traffic spike past the cap means lost telemetry exactly when it matters.
- **Tail sampling, if needed later, is operationally non-trivial.** Adding the `tailsamplingprocessor` requires re-tuning Collector resources and re-validating that lag-SLO queries remain representative. v1 ships with head-based 100% specifically to avoid this.
- **Application Insights' 90-day default retention** in the Log Analytics workspace is shorter than the ADR-0017 outbox window. Retention extension on the workspace costs more; the alignment between observability retention and outbox retention is deliberately not pursued — they're independent concerns.

### Out of scope

- **Real-User-Monitoring (RUM)** for the Admin Dashboard / Customer Portal frontends. Deferred to a frontend-stack ADR amendment when the dashboards ship.
- **Synthetic / blackbox probes.** Application Insights availability tests are sufficient for v1; standalone probes (e.g. for the Customer Portal embed) are deferred.
- **Service-level objective definitions in Azure Monitor SLO/SLI form.** v1 alerts are threshold-based; formal SLO/error-budget tracking arrives once production traffic is stable.
- **Cross-environment correlation** (sandbox events leaking into prod views, vice versa). Workspaces are environment-scoped by design; no cross-env queries.
- **Log archival to Storage** for cold logs. The 90-day workspace default is sufficient for v1 incident review; longer-horizon log retention is a separate ADR if compliance requires it.
- **Customer-managed encryption keys** for the Log Analytics workspace. Microsoft-managed keys until regulatory pressure surfaces, mirroring [ADR-0017](ADR-0017-outbox-retention-and-cold-archive.md).
