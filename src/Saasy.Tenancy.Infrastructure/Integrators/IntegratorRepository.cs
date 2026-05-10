using Microsoft.EntityFrameworkCore;
using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Infrastructure.Integrators;

internal sealed class IntegratorRepository(TenancyDbContext db) : IIntegratorRepository
{
    public async Task<Integrator?> GetByIdAsync(IntegratorId id, CancellationToken ct = default)
        => await db.Integrators.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<bool> ExistsAsync(IntegratorId id, CancellationToken ct = default)
        => await db.Integrators.AnyAsync(x => x.Id == id, ct);

    public void Add(Integrator integrator)
        => db.Integrators.Add(integrator);

    public async Task<IReadOnlyList<Integrator>> GetByApiKeyPrefixAsync(
        string last4,
        CancellationToken ct = default)
        => await db.Integrators
            .Where(i => i.ApiKeys.Any(k => k.Last4 == last4))
            .ToListAsync(ct);
}
