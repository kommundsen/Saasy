using Microsoft.EntityFrameworkCore;
using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Infrastructure;

public sealed class TenancyDbContext(DbContextOptions<TenancyDbContext> options) : DbContext(options)
{
    public DbSet<SchemaVersion> SchemaVersions => Set<SchemaVersion>();
    public DbSet<Integrator> Integrators => Set<Integrator>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("tenancy");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenancyDbContext).Assembly);
    }
}
