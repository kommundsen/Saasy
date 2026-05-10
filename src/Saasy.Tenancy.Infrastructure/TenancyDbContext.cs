using Microsoft.EntityFrameworkCore;
using Saasy.Tenancy.Domain.Customers;
using Saasy.Tenancy.Domain.Integrators;
using Saasy.Tenancy.Infrastructure.Audit;

namespace Saasy.Tenancy.Infrastructure;

public class TenancyDbContext(DbContextOptions<TenancyDbContext> options) : DbContext(options)
{
    public DbSet<SchemaVersion> SchemaVersions => Set<SchemaVersion>();
    public DbSet<Integrator> Integrators => Set<Integrator>();
    public DbSet<Customer> Customers => Set<Customer>();
    internal DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("tenancy");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenancyDbContext).Assembly);
    }
}
