using Microsoft.EntityFrameworkCore;

namespace Saasy.Tenancy.Infrastructure.Persistence;

public sealed class TenancyDbContext(DbContextOptions<TenancyDbContext> options) : DbContext(options)
{
    internal DbSet<MigrationSentinel> MigrationSentinels => Set<MigrationSentinel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("tenancy");

        modelBuilder.Entity<MigrationSentinel>(e =>
        {
            e.ToTable("__migration_sentinel");
            e.HasKey(x => x.Id);
            e.Property(x => x.AppliedAt).IsRequired();
            e.HasData(new MigrationSentinel { Id = 1, AppliedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
        });

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenancyDbContext).Assembly);
    }
}
