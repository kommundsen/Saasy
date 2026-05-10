using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Infrastructure.Integrators;

// Port for minting a per-Integrator SAS publish-only connection string against the
// Event Hubs management API. Implemented in the Infrastructure layer so the Domain
// never touches transport-layer concerns.
public interface IIngestionCredentialMintService
{
    // Returns a plaintext SAS connection string scoped to the given Integrator.
    // The caller is responsible for encrypting the result before persistence.
    Task<string> MintAsync(IntegratorId integratorId, CancellationToken ct = default);
}
