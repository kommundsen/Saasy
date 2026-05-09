# Saasy

Saasy is a metered-billing platform for B2B SaaS companies. It ingests usage Events, aggregates them into Rollups, applies Pricing Components, and generates Invoices.

## Project naming convention

Projects follow the pattern `Saasy.<Context>.<Layer>`:

| Segment | Values | Examples |
|---|---|---|
| `<Context>` | `SharedKernel`, `Tenancy`, `Catalog`, `Subscriptions`, `Metering`, `Invoicing`, `Delivery` | `Saasy.Tenancy`, `Saasy.Metering` |
| `<Layer>` | `Domain`, `Application`, `Infrastructure` | `Saasy.Tenancy.Domain` |

Host projects omit `<Layer>`: `Saasy.Api`, `Saasy.AppHost`, `Saasy.Worker`.

Test projects mirror the source project they exercise with a suffix:

| Suffix | Framework | Contains |
|---|---|---|
| `.Tests` | xUnit v3 | Example-based tests (`[Fact]`) |
| `.PropertyTests` | xUnit v3 + Conjecture | Property-based tests (`[Property]`) |

## Layer dependency rules

Dependencies flow inward. Each layer may only reference layers closer to the centre:

```
Saasy.AppHost
  orchestrates (not depends on) →  Saasy.Api  /  Saasy.Workers.*

Saasy.Api  /  Saasy.Workers.*
  depends on  →  {Context}.Infrastructure

{Context}.Infrastructure
  depends on  →  {Context}.Application

{Context}.Application
  depends on  →  {Context}.Domain

{Context}.Domain
  depends on  →  Saasy.SharedKernel.Domain

Saasy.SharedKernel.Domain
  depends on  →  BCL only (zero NuGet references)
```

Full detail in [docs/architecture/architecture.md](docs/architecture/architecture.md).

## Running locally

```sh
dotnet run --project src/Saasy.AppHost
```

Requires Docker (for the Postgres container the Aspire AppHost provisions in dev).

## Building and testing

```sh
dotnet build
dotnet test
```

Both must pass with zero warnings and zero failures on every PR.
