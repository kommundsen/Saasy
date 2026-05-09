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
}
