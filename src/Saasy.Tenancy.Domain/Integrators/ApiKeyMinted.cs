namespace Saasy.Tenancy.Domain.Integrators;

public sealed record ApiKeyMinted(
    IntegratorId IntegratorId,
    ApiKeyId ApiKeyId,
    string Name,
    string Last4,
    DateTime OccurredOn) : IDomainEvent;
