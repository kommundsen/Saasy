---
id: ADR-0002
type: adr
state: accepted
title: .NET 10 + Aspire + ASP.NET Core for backend services
date: 2026-05-03
deciders: [kim]
---

# ADR-0002 — .NET 10 + Aspire + ASP.NET Core for backend services

## Context

Saasy hosts a public API, Rollup workers, an Invoice generator, and a Webhook dispatcher. Drivers: strong Azure fit (Event Hubs, EF Core on Postgres), strongly-typed domain model for invoice correctness, author's existing footprint, and Aspire's dev-time orchestration value (service discovery, OpenTelemetry, dashboard) for a multi-service backend.

## Decision

Use **.NET 10 + .NET Aspire + ASP.NET Core Web API**.

- Aspire AppHost composes services; each backend service is an Aspire-orchestrated component.
- Public APIs use ASP.NET Core Minimal APIs (decided per endpoint cluster).
- Worker services (Rollup, InvoiceGenerator, WebhookDispatcher) are `BackgroundService` hosts.
- OpenTelemetry tracing/metrics/logging via Aspire defaults.
- Domain model uses C# records and immutable types where natural (Plan Versions, Invoices).

## Consequences

- **Productivity.** Aspire's dashboard + service discovery removes multi-service dev friction.
- **Performance.** .NET 10 is competitive with Go for Saasy's HTTP + DB + queue I/O shape.
- **Lock-in.** None on .NET itself. Some on Aspire — services can run without it (it's a dev-time orchestrator).
- **Risk.** .NET 10 (released Nov 2025) is early in its lifecycle. Stay current on patches; avoid preview features.
