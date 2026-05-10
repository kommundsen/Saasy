namespace Saasy.Tenancy.Domain.Integrators;

public sealed record IngestionCredentialMinted(
    IntegratorId IntegratorId,
    IngestionCredentialId IngestionCredentialId,
    DateTime OccurredOn) : IDomainEvent;
