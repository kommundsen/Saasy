namespace Saasy.Tenancy.Domain.Integrators;

public interface IIntegratorRepository
{
    Task<Integrator?> GetByIdAsync(IntegratorId id, CancellationToken ct = default);
    Task<bool> ExistsAsync(IntegratorId id, CancellationToken ct = default);
    void Add(Integrator integrator);

    // Returns all Integrators that own at least one ApiKey with the given last-4 prefix.
    // Multiple results are possible on a prefix collision (rare but handled by the caller).
    Task<IReadOnlyList<Integrator>> GetByApiKeyPrefixAsync(string last4, CancellationToken ct = default);
}
