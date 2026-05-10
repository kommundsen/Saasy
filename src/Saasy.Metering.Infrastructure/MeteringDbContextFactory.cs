using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Saasy.Metering.Infrastructure;

internal sealed class MeteringDbContextFactory : IDesignTimeDbContextFactory<MeteringDbContext>
{
    public MeteringDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MeteringDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=saasy;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema: "metering"))
            .Options;

        return new MeteringDbContext(options);
    }
}
