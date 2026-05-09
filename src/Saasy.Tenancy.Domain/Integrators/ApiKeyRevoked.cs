namespace Saasy.Tenancy.Domain.Integrators;

public sealed record ApiKeyRevoked(
    IntegratorId IntegratorId,
    ApiKeyId ApiKeyId,
    DateTime OccurredOn) : IDomainEvent;
