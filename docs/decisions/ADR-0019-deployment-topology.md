---
id: ADR-0019
type: adr
state: accepted
title: Deployment topology
date: 2026-05-08
deciders: [kim]
---

# ADR-0019 — Deployment topology

## Context

[ADR-0002](ADR-0002-backend-stack.md) commits to .NET + Aspire for Api and Worker services. [ADR-0018](ADR-0018-observability-sinks.md) requires a sidecar OpenTelemetry Collector per service instance in deployed environments and defers Collector deployment shape to this ADR. Iteration 07 ships the first production deploy (Invoices + Webhooks) and is gated on a chosen hosting target.

The candidates considered:

- **Azure Container Apps (ACA)** — Aspire's first-class deployment target via `azd`. Managed Kubernetes underneath, KEDA-based event-driven scaling for Service Bus / Event Hubs / HTTP, scales-to-zero, no cluster operations.
- **Azure Kubernetes Service (AKS)** — full Kubernetes. Required when multi-product / multi-team workloads share a cluster, when networking or security needs exceed ACA's envelope, or when you want full control over node pools and addons.
- **App Service** — Api fits adequately; Worker fits poorly (Service Bus session-aware competing consumers per [ADR-0008](ADR-0008-internal-domain-event-bus.md) are not a first-class App Service pattern). Aspire's deployment story does not target App Service as a first-class option.

This ADR settles the target as **Azure Container Apps** for v1, and pins the operational specifics (environment topology, scaling, image supply chain, secrets, observability sidecar, deploy pipeline).

## Decision

### Target — Azure Container Apps for Api and Worker

Both the Api service (HTTP, public ingress) and the Worker service (no ingress, Service Bus and Event Hubs consumers) run as Container Apps. The Aspire AppHost generates the Bicep manifest via `azd infra synth`; the synthesised Bicep is committed to the repo and is the source of truth for deployment.

App Service is rejected. AKS is documented as the migration target if and only if the v1 deployment hits a real ACA ceiling (specific networking, sidecar daemon, multi-team cluster sharing). No speculative AKS work in v1.

### Environment topology — one Container Apps Environment per Saasy environment

- **Sandbox**: `cae-saasy-sandbox` Container Apps Environment in one region. Scale-to-zero permitted for both Api and Worker. Cold-start latency on the first request after a quiet period is acceptable; sandbox is not load-tested SLO-bearing.
- **Production**: `cae-saasy-prod` Container Apps Environment in the same region as Postgres / Service Bus / Event Hubs. **Minimum 2 replicas** for both Api and Worker. Multi-region failover is out of scope for v1.
- **No shared environment between sandbox and production.** Per [ADR-0009](ADR-0009-integrator-per-kind-environment-model.md) sandbox and production Integrators are entirely separate; the deployment topology preserves that boundary at the network layer too.
- **One Log Analytics Workspace per environment** ([ADR-0018](ADR-0018-observability-sinks.md)). The Container Apps Environment binds to the matching workspace for ACA's built-in log capture (stdout/stderr); telemetry goes via the OTel Collector sidecar to the per-service Application Insights resource, not via the ACA log capture.

### Scaling rules

- **Api** — HTTP-concurrency rule. Scale on concurrent requests per replica, target 50, min 2 (prod) / 0 (sandbox), max 10 (initial; tunable per traffic shape).
- **Worker** — multiple KEDA scalers, OR-combined:
  - **Service Bus session-aware** scaler on each subscription the worker consumes from. Target one replica per ~5 active sessions, min 2 (prod) / 0 (sandbox), max bounded by Postgres connection budget (below).
  - **Event Hubs** scaler on partition lag for the ingest hub.
  - HTTP scaling disabled — the worker has no public ingress.
- **Per-Integrator FIFO** ([ADR-0008](ADR-0008-internal-domain-event-bus.md)) is enforced inside the worker process via single-active-dispatcher-per-`(context, integrator_id)` leader election, **not** by capping replica count. KEDA can scale workers freely; the worker code handles the ordering invariant.
- **Postgres connection budget** is the hard scaling ceiling for Worker replicas. Each worker replica reserves N pooled connections; max replicas × N must stay under the Postgres Flexible Server connection limit with headroom for Api and operator queries. The worker's `appsettings` carries a tuned pool size; max-replicas in the scale rule is derived from it.

### Image supply chain — Azure Container Registry per environment

- **Two ACR instances**: `acrsaasysandbox`, `acrsaasyprod`. Production never pulls from the sandbox registry.
- Images are tagged `<service>:<git-sha>`; `latest` tags are not used in deployments.
- ACA pulls via **managed identity** authenticated to ACR (`AcrPull` role). No registry credentials in pipelines or app config.
- A retention policy on each ACR keeps the most recent 50 image versions per service plus anything tagged with a git ref currently deployed.

### Secrets — Key Vault references via Container Apps secrets

- All sensitive config (Postgres connection strings, Service Bus / Event Hubs connection details when not using managed identity, signing keys, Integrator OIDC client secrets per [ADR-0007](ADR-0007-admin-dashboard-identity-and-customer-portal-posture.md)) lives in **Azure Key Vault**.
- Container Apps reference secrets by Key Vault URI; ACA fetches the value at revision creation. Rotation is a Key Vault concern; rotated secrets require a revision restart, which `azd` handles.
- **Managed identity is used wherever the resource supports it**: Postgres, Service Bus, Event Hubs, Storage, ACR. Connection strings are the fallback for resources that don't yet support Entra ID auth.
- No secret values appear in environment variables, Bicep, or commit history. The Bicep references Key Vault URIs, not values.

### OTel Collector — sidecar via Container Apps multi-container template

[ADR-0018](ADR-0018-observability-sinks.md) mandates an OTel Collector sidecar per service instance. ACA supports multi-container apps where the application container and the Collector run in the same revision and share `localhost`.

- Each Container App for Api and Worker includes a second container running `otel/opentelemetry-collector-contrib` with the Saasy Collector config (PII filter, Azure Monitor exporter, App-Insights connection string from Key Vault).
- The application container ships OTLP to `localhost:4317`; the Collector exports to Application Insights.
- Collector resource limits: 100m CPU, 128Mi memory. Profiled and adjusted post-Iter-07 if necessary.
- Collector image version is pinned in Bicep; upgrades are explicit.

### Deploy pipeline — `azd` driving Bicep, GitHub Actions for CI/CD

- **Local dev**: `azd up` provisions and deploys end-to-end against a dev subscription.
- **Sandbox**: GitHub Actions workflow on push to `main`. Federated identity (OIDC) from GitHub to Entra ID; no service principal secrets stored in GitHub.
- **Production**: GitHub Actions workflow on tagged release. Approval gate before the deploy step.
- **Revision strategy**: Container Apps' "Multiple revisions" mode. New deploy creates a revision at 0% traffic; smoke-test job hits the revision-specific URL; on success, traffic shifts 100% to the new revision. Rollback is a `az containerapp revision activate` against the previous revision.
- **No blue/green at the environment level** — revision-based traffic split serves the same purpose with less infrastructure.
- **Database migrations** run as a separate Container App **Job** (not part of either service's revision lifecycle) gated to run before traffic shifts to a new revision. Migrations are forward-only and idempotent, and ship in their own image.

### Out-of-band: networking and DNS

- **Public ingress** for the Api Container App on `api.saasy.example` (sandbox: `api.sandbox.saasy.example`). Custom domains bound via ACA's managed certificates.
- **Internal ingress disabled** for the Worker. The Worker has no public surface; operator access is via `az containerapp exec` behind RBAC.
- Container Apps Environment uses **VNet integration** in production; sandbox runs on the default ACA-managed network. Production VNet integration unblocks future Private Endpoint-based connectivity to Postgres / Key Vault / ACR if required.

### Disaster recovery

- **Region**: single region (matching the rest of the Azure resources). Multi-region active/active is out of scope.
- **Recovery from region outage**: documented runbook for redeploying ACA into a paired region pointed at restored Postgres + Service Bus resources. RPO/RTO targets are not formally committed in this ADR; they belong in a separate DR ADR if/when business commitments require it.
- **Application-level recovery** (revision rollback, restart on crash loop) is automatic via ACA's revision and replica machinery.

## Consequences

### Wins

- **Aspire's deployment story Just Works.** `azd up` and `azd deploy` produce a working environment without bespoke wiring; the synthesised Bicep is the only IaC layer.
- **No cluster operations.** No node-pool upgrades, no addon management, no kubelet on-call. ACA absorbs the platform-engineering burden a v1 product team can't justify staffing.
- **KEDA-based scaling is event-aware out of the box.** Worker replicas track Service Bus session count and Event Hubs lag without custom scaler code.
- **Sidecar Collector pattern is supported as-is.** [ADR-0018](ADR-0018-observability-sinks.md)'s sidecar mandate maps directly to ACA's multi-container template; no compromises on the observability shape.
- **Managed identity end-to-end** removes most secret-handling surface. Key Vault references cover the remainder, audit-logged on resolution.
- **Revision-based traffic split** is cheaper, simpler, and faster than parallel environment blue/green.
- **The two-environment isolation** mirrors the [ADR-0009](ADR-0009-integrator-per-kind-environment-model.md) sandbox-vs-production boundary at the network layer, removing accidental cross-environment access.

### Costs

- **ACA's networking ceiling is lower than AKS's.** Specific patterns (DaemonSets, custom CNI, raw IPv6) aren't supported. None matter for v1 Saasy; if they ever do, the migration target (AKS) is documented but not pre-built.
- **Per-Integrator FIFO is enforced in the worker, not by ACA.** Replica count can scale freely; the worker leader-election machinery is mandatory and non-trivial. Bug surface is real and lives in [ADR-0008](ADR-0008-internal-domain-event-bus.md)-§-aware code.
- **Postgres connection budget is the scaling ceiling.** Adding worker replicas above the connection budget breaks the database, not the worker. The max-replicas knob is hand-tuned and must be revisited as worker pool size or Postgres tier changes.
- **Multi-region failover is unbuilt.** A regional outage means Saasy is down until the documented runbook completes. Acceptable for a v1 metering-and-invoicing product targeting business hours; not acceptable for a real-time payments product (Saasy is not one).
- **Container Apps' opinions about revisions are firm.** Multi-revision mode adds DNS routing complexity for revision-specific smoke tests, and revision lifecycle (deletion, traffic redistribution) is ACA's call, not ours. The first time this surprises someone in production, the runbook is the answer.
- **Sidecar Collector adds 100m / 128Mi per replica.** At max-replica counts in production this is meaningful but bounded.
- **`azd` and the synthesised Bicep are coupled.** Manual edits to `infra/*.bicep` are overwritten by the next `azd infra synth`. The repo carries explicit guidance: the AppHost is the source, the Bicep is generated, ad-hoc Bicep edits must move into the AppHost or live in a separate handcrafted module imported by the synthesised root.

### Out of scope

- **Multi-region active/active** deployment.
- **AKS migration** path beyond a documented "if we hit a ceiling" sentence. No parallel AKS infrastructure pre-built.
- **Dapr.** Aspire integrates with Dapr on ACA, but Saasy uses Service Bus and Event Hubs SDKs directly per [ADR-0008](ADR-0008-internal-domain-event-bus.md). Dapr is not adopted.
- **Container Apps Jobs for non-migration workloads** (scheduled archive per [ADR-0017](ADR-0017-outbox-retention-and-cold-archive.md), Period Close orchestration). The archive job ships as a Container App Job; that's an iteration-level decision, not an ADR concern.
- **Edge / CDN** for Api or static-asset hosting. Frontend distribution is a frontend-stack concern; the Admin Dashboard and Customer Portal SPAs deploy via Static Web Apps or equivalent, separate from the Api Container App.
- **Customer-managed encryption keys (CMK)** for ACA's managed environment. Microsoft-managed keys, mirroring [ADR-0017](ADR-0017-outbox-retention-and-cold-archive.md) and [ADR-0018](ADR-0018-observability-sinks.md).
