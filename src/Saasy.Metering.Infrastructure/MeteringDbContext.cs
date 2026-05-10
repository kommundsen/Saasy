using Microsoft.EntityFrameworkCore;
using Saasy.Metering.Infrastructure.Events;

namespace Saasy.Metering.Infrastructure;

public sealed class MeteringDbContext(DbContextOptions<MeteringDbContext> options) : DbContext(options)
{
    public DbSet<MeteringEvent> Events => Set<MeteringEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("metering");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MeteringDbContext).Assembly);
    }
}
