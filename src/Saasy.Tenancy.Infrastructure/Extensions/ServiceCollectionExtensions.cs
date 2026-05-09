using Microsoft.Extensions.Hosting;
using Saasy.Tenancy.Infrastructure.Persistence;

namespace Saasy.Tenancy.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddTenancyInfrastructure(
        this IHostApplicationBuilder builder,
        string connectionName = "saasy")
    {
        builder.AddNpgsqlDbContext<TenancyDbContext>(connectionName);
        return builder;
    }
}
