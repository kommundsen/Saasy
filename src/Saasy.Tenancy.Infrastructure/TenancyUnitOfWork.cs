using Saasy.Tenancy.Application;

namespace Saasy.Tenancy.Infrastructure;

internal sealed class TenancyUnitOfWork(TenancyDbContext db) : IUnitOfWork
{
    public async Task CommitAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
