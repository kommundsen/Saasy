using Saasy.Tenancy.Domain;

namespace Saasy.Tenancy.Domain.Integrators;

public sealed record IntegratorTierChanged(
    IntegratorId IntegratorId,
    IntegratorTier OldTier,
    IntegratorTier NewTier,
    DateTime OccurredOn) : IDomainEvent;
