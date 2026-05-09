namespace Saasy.Tenancy.Domain.Integrators;

public interface IIntegratorRepository
{
    Task<Integrator?> GetByIdAsync(IntegratorId id, CancellationToken ct = default);
    Task<bool> ExistsAsync(IntegratorId id, CancellationToken ct = default);
    void Add(Integrator integrator);
}
