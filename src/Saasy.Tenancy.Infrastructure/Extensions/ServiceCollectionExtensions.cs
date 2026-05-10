using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Saasy.Tenancy.Application;
using Saasy.Tenancy.Application.Customers;
using Saasy.Tenancy.Application.Integrators;
using Saasy.Tenancy.Domain.Customers;
using Saasy.Tenancy.Domain.Integrators;
using Saasy.Tenancy.Infrastructure.Audit;
using Saasy.Tenancy.Infrastructure.Auth;
using Saasy.Tenancy.Infrastructure.Customers;
using Saasy.Tenancy.Infrastructure.Integrators;

namespace Saasy.Tenancy.Infrastructure.Extensions;

// -- How to register a per-context DbContext in later iterations --
//
// Pattern for each bounded context's Infrastructure project:
//
// 1. Add an AddXxxInfrastructure() extension on IHostApplicationBuilder:
//
//      builder.AddNpgsqlDbContext<XxxDbContext>(
//          connectionName: "saasy",       // matches AppHost .AddDatabase("saasy")
//          configureDbContextOptions: opts =>
//              opts.UseNpgsql(o => o.MigrationsHistoryTable(
//                  "__EFMigrationsHistory", schema: "xxx")));
//
// 2. Expose a MigrateXxxAsync() extension on IServiceProvider so hosts do not
//    take a direct EF Core dependency. The DbContext is registered as scoped,
//    so the helper MUST create a scope before resolving. The migrations history
//    table is configured to live in the per-context schema (so each context owns
//    its own migration log), which means the schema MUST exist before EF can
//    read the history -- ensure it explicitly before MigrateAsync:
//
//      public static async Task MigrateXxxAsync(this IServiceProvider services)
//      {
//          await using var scope = services.CreateAsyncScope();
//          var db = scope.ServiceProvider.GetRequiredService<XxxDbContext>();
//          await db.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS xxx;");
//          await db.Database.MigrateAsync();
//      }
//
// 3. In Program.cs of Saasy.Api / each Saasy.Workers.* host:
//
//      builder.AddTenancyInfrastructure();
//      // ...
//      await app.Services.MigrateTenancyAsync();
//
// -- End pattern documentation --

public static class ServiceCollectionExtensions
{
    // Call from the Api host -- resolves ICurrentActor from HttpContext.User claims.
    public static IHostApplicationBuilder AddTenancyInfrastructure(
        this IHostApplicationBuilder builder)
    {
        AddCommonTenancyInfrastructure(builder);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentActor, HttpContextCurrentActor>();

        builder.Services
            .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationHandler.SchemeName, _ => { });

        builder.Services.AddAuthorization();

        return builder;
    }

    // Call from Worker hosts -- uses a stable "system:worker" identity.
    public static IHostApplicationBuilder AddTenancyInfrastructureForWorker(
        this IHostApplicationBuilder builder)
    {
        AddCommonTenancyInfrastructure(builder);
        builder.Services.AddScoped<ICurrentActor, WorkerCurrentActor>();
        return builder;
    }

    private static void AddCommonTenancyInfrastructure(IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<TenancyDbContext>(
            connectionName: "saasy",
            configureDbContextOptions: opts =>
                opts.UseNpgsql(npgsql =>
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema: "tenancy")));

        builder.Services.AddScoped<IIntegratorRepository, IntegratorRepository>();
        builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
        builder.Services.AddScoped<TenancyAuditWriter>();
        builder.Services.AddScoped<IAuditWriter>(sp => sp.GetRequiredService<TenancyAuditWriter>());
        builder.Services.AddScoped<IUnitOfWork, TenancyUnitOfWork>();

        // Placeholder -- StubIngestionCredentialMintService throws NotImplementedException.
        // Replace with a real implementation in iter-02 before minting credentials in production.
        builder.Services.AddSingleton<IIngestionCredentialMintService, StubIngestionCredentialMintService>();
        builder.Services.AddScoped<MintApiKey.Handler>();
        builder.Services.AddScoped<CreateCustomer.Handler>();
        builder.Services.AddScoped<GetCustomer.Handler>();
        builder.Services.AddScoped<RenameCustomer.Handler>();
    }

    public static async Task MigrateTenancyAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

        // The migration history table lives in the per-context schema, so the schema
        // must exist before EF Core can read it on a fresh database. The InitialSchema
        // migration's own EnsureSchema is what populates this on rerun, but EF reads
        // the history before applying any migration.
        await db.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS tenancy;");

        await db.Database.MigrateAsync();
    }
}
