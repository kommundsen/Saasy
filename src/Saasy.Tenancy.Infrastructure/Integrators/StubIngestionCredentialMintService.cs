using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Infrastructure.Integrators;

// TODO(iter-02): Replace with a real implementation that calls the Event Hubs
// management API (Azure.ResourceManager.EventHubs -- AuthorizationRuleResource +
// RegenerateKeyAsync) to mint a Send-only SAS per Integrator.
// Tracked as a follow-up issue in iter-02 or later.
internal sealed class StubIngestionCredentialMintService : IIngestionCredentialMintService
{
    public Task<string> MintAsync(IntegratorId integratorId, CancellationToken ct = default)
        => throw new NotImplementedException(
            "Live Event Hubs SAS minting is not yet implemented. " +
            "Replace StubIngestionCredentialMintService with a real " +
            "IIngestionCredentialMintService before minting ingestion credentials in production. " +
            "See the TODO(iter-02) comment in this file.");
}
