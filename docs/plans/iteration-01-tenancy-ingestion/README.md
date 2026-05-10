# Iteration 01 — Tenancy & Ingestion

## Goal

Make the platform multi-tenant and able to receive raw usage Events. Integrators can be provisioned, ApiKeys minted, Customers created. Both ingestion paths (Event Hubs partitioned by `integrator_id`, plus the HTTP fallback endpoint) accept Events and persist them append-only. No Rollups, no Plans yet.

## Out of scope

No Plan/Subscription wiring. Events land in a raw store and are validated for shape only — semantic validation against Product Type / Dimension is deferred to Iteration 02–03. No Webhooks out.

## Exit criteria

- Integrator aggregate enforces Kind + Tier fixed at creation per [ADR-0009](../../decisions/ADR-0009-integrator-per-kind-environment-model.md).
- Customer aggregate created via Api, scoped under an Integrator.
- ApiKey is a Child Entity of Integrator; secret hashed at rest; revocation supported.
- Event Hub namespace + per-integrator partition strategy provisioned via Aspire (or emulator locally) per [ADR-0001](../../decisions/ADR-0001-ingestion-stream.md).
- HTTP `POST /v1/events` accepts the same Event payload shape and writes to the same append-only store.
- Authentication on Api uses the ApiKey (header lookup → hash compare → Integrator scope).
- Audit skeleton records actor, action, target across the above flows.

## Issues

- [x] [00 — Integrator aggregate (Kind, Tier, Timezone)](00-integrator-aggregate.md)
- [x] [01 — ApiKey lifecycle (hashed storage, revocation)](01-apikey-lifecycle.md)
- [x] [02 — Customer aggregate](02-customer-aggregate.md)
- [x] [03 — ApiKey authentication middleware](03-apikey-auth-middleware.md)
- [x] [04 — Event Hubs provisioning + partition-by-integrator](04-event-hubs-provisioning.md)
- [x] [05 — Event ingestion consumer (raw append-only store)](05-event-ingestion-consumer.md)
- [x] [06 — HTTP `POST /v1/events` ingestion endpoint](06-http-event-ingestion.md)
- [x] [07 — Audit skeleton](07-audit-skeleton.md)
