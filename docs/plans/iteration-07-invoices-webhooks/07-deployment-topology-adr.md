---
title: Deployment topology ADR + first prod deploy
iteration: 07
status: todo
labels: [infrastructure, adr, deployment]
depends-on: []
---

# Deployment topology ADR + first prod deploy

Decide between Azure App Service, Container Apps, and AKS for `Saasy.Api` and `Saasy.Worker`. Record decision, then ship a staging deploy that the v1 stack runs on end-to-end (ingest → rollup → final close → invoice → webhook).

## Acceptance criteria

- ADR written under `docs/decisions/` (next available number).
- IaC module (Bicep / Terraform) creates the chosen runtime + Postgres + Service Bus + Event Hubs + storage.
- CI/CD pipeline deploys to `staging` from `main`.
- Smoke test (E2E POST event → webhook delivered) passes against staging.
- Crossed off in [iterations.md](../iterations.md) "Open decisions".
