# Saasy Implementation Iterations

Source-of-truth tracker for the implementation roadmap. Each iteration is a coherent vertical slice that can be planned, executed, and demoed as a unit. Individual issues live in the per-iteration folder and carry their own `status` frontmatter; this file tracks iteration-level progress only.

## Status legend

- `[ ]` — not started
- `[~]` — in progress
- `[x]` — done

Issue-level statuses (in frontmatter): `todo`, `in-progress`, `blocked`, `done`.

## Open decisions (pre-requisite)

These need a written ADR before the iterations that depend on them ship. Tracked here, not as iteration issues.

- [x] **ADR-0007** — Admin Dashboard identity & Customer Portal posture: ASP.NET Core Identity + per-Integrator OIDC SSO for Admin; Customer Portal is Integrator-controlled only (`IntegratorOwned` JWT embed or `Federated` OIDC). `saasy-idp` mode dropped. Authored at [docs/decisions/ADR-0007](../decisions/ADR-0007-admin-dashboard-identity-and-customer-portal-posture.md).
- [x] **ADR-0014** — Cross-context write/consistency model (delete-after-create, projection lag, HTTP 425 semantics). Authored at [docs/decisions/ADR-0014](../decisions/ADR-0014-cross-context-write-and-consistency-model.md).
- [x] **ADR-0017** — Outbox retention + cold archive: 90-day hot window in Postgres, then JSONL.gz on Azure Blob (Cool → Archive at 12mo, prune at 7y), evidence-only replay. Authored at [docs/decisions/ADR-0017](../decisions/ADR-0017-outbox-retention-and-cold-archive.md).
- [x] **ADR-0018** — Observability sinks: OTel Collector → Azure Monitor / Application Insights, one Log Analytics Workspace per environment, Aspire dashboard for local-dev. Authored at [docs/decisions/ADR-0018](../decisions/ADR-0018-observability-sinks.md).
- [x] **ADR-0019** — Deployment topology: Azure Container Apps for Api + Worker, two ACA Environments (sandbox/prod), KEDA event-driven scaling, sidecar OTel Collector, `azd`-driven Bicep, revision-based traffic shifts. Authored at [docs/decisions/ADR-0019](../decisions/ADR-0019-deployment-topology.md).
- [x] **ADR-0020** — Event Hubs Capture: enable on ingest hubs from Iter 01, Avro to Blob with Cool→Archive→delete-at-7y lifecycle, default 5min/300MB windows, operator-driven scan-and-rewrite for GDPR. Authored at [docs/decisions/ADR-0020](../decisions/ADR-0020-event-hubs-capture.md).

## Iterations

- [x] [00 — Foundations](iteration-00-foundations/README.md) — solution layout, Aspire AppHost, Postgres, SharedKernel, Conjecture.NET, CI.
- [x] [01 — Tenancy & Ingestion](iteration-01-tenancy-ingestion/README.md) — Integrator, Customer, ApiKey aggregates; Event Hub + HTTP ingest; raw Event landing.
- [ ] [02 — Catalog](iteration-02-catalog/README.md) — Product Type, Dimension, Plan, PlanVersion, Pricing Components (Flat Fee, Per-Seat Fee, flat-rate Metered Charge).
- [ ] [03 — Subscriptions & Rollups](iteration-03-subscriptions-rollups/README.md) — Subscription state machine, Rollups, Outbox, Service Bus, projections.
- [ ] [04 — Admin Dashboard (read-only)](iteration-04-admin-dashboard-readonly/README.md) — React + Vite shell, OpenAPI client, read-only views over all current aggregates.
- [ ] [05 — Quota, Overage & Thresholds](iteration-05-quota-overage-thresholds/README.md) — Quota + Overage on Metered Charges; Threshold firing rules; `usage.threshold.crossed` event.
- [ ] [06 — Period Boundaries, Final Close & Plan Transitions](iteration-06-period-final-close-transitions/README.md) — Cycle Anchor, Late Event Window, Final Close, Plan Transitions with Proration.
- [ ] [07 — Invoices & Webhooks](iteration-07-invoices-webhooks/README.md) — Invoice + LineItem + CreditNote, JSON + HTML rendering, Webhook delivery pipeline.
- [ ] [08 — Tiered Pricing](iteration-08-tiered-pricing/README.md) — Graduated + Volume tier ladders on Metered Charges.
- [ ] [09 — Customer Portal](iteration-09-customer-portal/README.md) — Embeddable iframe + white-label standalone build targets, per-Integrator theming.
- [ ] [10 — Identity Modes](iteration-10-identity-modes/README.md) — `IntegratorOwned` signed JWTs and `Federated` (OIDC) for Customer Portal; ASP.NET Core Identity + per-Integrator OIDC SSO for Admin Dashboard.
- [ ] [11 — Commitments, Minimums & Credits](iteration-11-commitments-minimums-credits/README.md) — Pre-purchased usage/revenue, per-Billing-Period floors, prepaid balance drawdown.

## Notes

- Iterations are **sequential by default** — dependencies are noted in each iteration README. Where parallelism is safe (e.g. 04 read-only Dashboard while 05 is in flight) it is called out explicitly.
- Iteration scope is fixed at start; new work surfaced mid-iteration becomes a new issue in a later iteration unless it is a hard blocker.
- Property-based tests ([ADR-0005](../decisions/ADR-0005-property-based-testing.md)) are mandatory in iterations 03, 05, 06, 07, 08, 11.
- UI work in iterations 04, 09, and any later surface work pulls from the [Saasy Design System](../design/README.md) (`saasy-design-system` skill, tokens in [colors_and_type.css](../design/colors_and_type.css), surface kits under [ui_kits/](../design/ui_kits/)).
