# Saasy

Usage-based billing infrastructure for B2B SaaS Integrators.

## Project structure

```
src/
  Saasy.SharedKernel.Domain/       Cross-context Value Objects (Money, Currency). Zero NuGet refs.
  Saasy.<Context>.Domain/          Aggregate Roots, Value Objects, Domain Events, Domain Services, Repository interfaces.
  Saasy.<Context>.Application/     Vertical Slices, Commands, Queries, Handlers, IUnitOfWork.
  Saasy.<Context>.Infrastructure/  EF Core DbContexts, Repository adapters, UnitOfWork, Outbox interceptor.
  Saasy.Api/                       ASP.NET Core API host. Thin composition root.
  Saasy.Worker/                    Worker host (BackgroundService). Thin composition root.
  Saasy.AppHost/                   .NET Aspire AppHost. Orchestrates all processes and resources.

tests/
  Saasy.<Context>.Domain.Tests/         Example-based tests (xUnit) for domain invariants.
  Saasy.<Context>.Domain.PropertyTests/ Property-based tests (Conjecture) for universal domain claims.
  Saasy.<Context>.Application.Tests/    Handler tests using hand-written fakes.
```

## Naming convention

`Saasy.<Context>.<Layer>` -- e.g. `Saasy.Tenancy.Domain`, `Saasy.Catalog.Application`.

Bounded contexts: `Tenancy`, `Catalog`, `Subscriptions`, `Metering`, `Invoicing`, `Delivery`.

Layers: `Domain`, `Application`, `Infrastructure`.

Test projects append `.Tests` (example-based) or `.PropertyTests` (property-based) to the project they test.

## Layer dependency rules

```
Saasy.SharedKernel.Domain
  <- Saasy.<Context>.Domain
       <- Saasy.<Context>.Application
            <- Saasy.<Context>.Infrastructure
                 <- Saasy.Api | Saasy.Worker.*
                      <- Saasy.AppHost (runtime orchestration only)
```

Dependencies point inward. The Domain layer has zero NuGet references. Cross-context references use IDs only -- never navigation properties, never FK constraints.

See [docs/architecture/architecture.md](docs/architecture/architecture.md) for the full architectural reference and [CONTEXT.md](CONTEXT.md) for domain vocabulary.

## Running locally

```sh
dotnet run --project src/Saasy.AppHost
```

Requires Docker (for Postgres container) and the .NET Aspire workload (`dotnet workload install aspire`).

## Building and testing

```sh
dotnet build
dotnet test
```
