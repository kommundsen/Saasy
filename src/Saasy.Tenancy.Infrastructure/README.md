# Saasy.Tenancy.Infrastructure

Infrastructure layer for the Tenancy bounded context. Contains the EF Core DbContext, migrations, and the service registration helper.

## DbContext

`TenancyDbContext` uses `HasDefaultSchema("tenancy")`. All Tenancy entity tables live under the `tenancy` Postgres schema. Configurations are applied via `ApplyConfigurationsFromAssembly`, so adding a new `IEntityTypeConfiguration<T>` in this project automatically registers it.

## Service registration

`AddTenancyInfrastructure(IHostApplicationBuilder, string connectionName)` in `Extensions/ServiceCollectionExtensions.cs` registers the `TenancyDbContext` via the Aspire `AddNpgsqlDbContext<T>` integration. The `connectionName` must match the Aspire AppHost resource name (default: `"saasy"`).

Call it from the composition root:

```csharp
builder.AddTenancyInfrastructure();
```

## Migrations

Migrations live in `Persistence/Migrations/`. Add new migrations from the repo root:

```sh
dotnet ef migrations add <Name> \
  --project src/Saasy.Tenancy.Infrastructure \
  --startup-project src/Saasy.Api \
  --output-dir Persistence/Migrations \
  --context Saasy.Tenancy.Infrastructure.Persistence.TenancyDbContext
```

Dev startup: `Saasy.Api` calls `db.Database.MigrateAsync()` at startup, so migrations run automatically against the Aspire-provisioned Postgres container in development.

Production: run migrations via the `Saasy.Migrate` console host in the deployment pipeline (future iteration).

## Pattern for subsequent bounded contexts

Each new bounded context follows the same structure:

1. Create `{Context}DbContext(DbContextOptions<{Context}DbContext> options)` with `HasDefaultSchema("{context}")`.
2. Add a `ServiceCollectionExtensions.cs` exposing `Add{Context}Infrastructure(IHostApplicationBuilder, string connectionName)` that calls `builder.AddNpgsqlDbContext<{Context}DbContext>(connectionName)`.
3. Add package references `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` and `Npgsql.EntityFrameworkCore.PostgreSQL` to the Infrastructure csproj (versions from `Directory.Packages.props`).
4. Create the first migration with the command above, substituting the context name.
5. Register the helper in `Saasy.Api/Program.cs` and the relevant Worker `Program.cs`.

The AppHost passes the single `"saasy"` database to all services; each context's schema isolates its tables within that database.
