using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
//    take a direct EF Core dependency:
//
//      public static Task MigrateXxxAsync(this IServiceProvider services) =>
//          services.GetRequiredService<XxxDbContext>().Database.MigrateAsync();
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
    public static IHostApplicationBuilder AddTenancyInfrastructure(
        this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<TenancyDbContext>(
            connectionName: "saasy",
            configureDbContextOptions: opts =>
                opts.UseNpgsql(npgsql =>
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema: "tenancy")));

        return builder;
    }

    public static Task MigrateTenancyAsync(this IServiceProvider services) =>
        services.GetRequiredService<TenancyDbContext>().Database.MigrateAsync();
}
