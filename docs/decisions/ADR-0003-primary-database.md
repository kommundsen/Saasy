---
id: ADR-0003
type: adr
state: accepted
title: PostgreSQL on Azure as the primary transactional database
date: 2026-05-03
deciders: [kim]
---

# ADR-0003 — PostgreSQL on Azure as the primary transactional database

## Context

Saasy stores Integrators, Customers, Plans, Plan Versions, Subscriptions, Rollups, Invoices, and Webhook delivery records. Workload is mostly OLTP with structured-but-flexible data (Pricing Components, Event `properties{}`); strong consistency is required for invoice computation. Raw Events live in Event Hubs (with Capture → Blob); only Rollups land in this database. v1 scale fits a single primary; design must permit read replicas and partitioning later.

PostgreSQL is the dominant database in modern billing platforms (Lago, Orb both use it).

## Decision

Use **Azure Database for PostgreSQL — Flexible Server** via **Entity Framework Core** with the **Npgsql** provider.

- One logical database per region. One schema per bounded context (`tenancy`, `catalog`, `subscriptions`, `metering`, `invoicing`, `delivery`) plus an `audit` schema for cross-cutting logs. Production and sandbox Integrators share the database; isolation is by `IntegratorId` (per [ADR-0009](ADR-0009-integrator-per-kind-environment-model.md)).
- JSONB for Plan Pricing Component graphs and Event-derived fields.
- Outbox pattern for Webhook dispatch and external side effects.
- EF Core migrations; PR review for any change touching invoice-affecting tables.

## Consequences

- **JSONB.** First-class JSON storage with indexable paths fits Pricing Components and per-Integrator extension fields.
- **Cost.** Lower than Azure SQL; Flexible Server scales vertically and adds replicas without re-architecture.
- **EF Core via Npgsql.** Mature; some PostgreSQL features (JSONB, full-text, advisory locks) need provider-specific code.
- **Portability.** PostgreSQL is the most portable choice if Azure ever needs to be left.
- **Scale.** Vertical scaling carries v1 and beyond. Sharding (by Integrator) is a post-v1 conversation.
