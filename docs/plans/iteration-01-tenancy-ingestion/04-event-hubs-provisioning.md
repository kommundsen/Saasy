---
title: Event Hubs provisioning + partition-by-integrator
iteration: 01
status: todo
labels: [ingestion, infrastructure, aspire]
depends-on: []
agent: backend
---

# Event Hubs provisioning + partition-by-integrator

Provision the hosted ingestion stream per [ADR-0001](../../decisions/ADR-0001-ingestion-stream.md): one namespace per region, one Event Hub `events`, partition key = `integrator_id`. Local dev uses the Event Hubs emulator under Aspire.

## Acceptance criteria

- Aspire AppHost runs Event Hubs emulator container locally.
- Bicep / Terraform module (or Aspire publish profile) for cloud provisioning of namespace + hub + consumer group `saasy-ingest`.
- SAS publish-only credentials minted per Integrator at provisioning time, stored encrypted.
- Documentation in `docs/architecture/` covers partition key choice + capacity assumptions.

## References

- [ADR-0001](../../decisions/ADR-0001-ingestion-stream.md)
