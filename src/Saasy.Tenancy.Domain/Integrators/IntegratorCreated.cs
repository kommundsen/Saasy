using Saasy.Tenancy.Domain;

namespace Saasy.Tenancy.Domain.Integrators;

public sealed record IntegratorCreated(
    IntegratorId IntegratorId,
    string Name,
    IntegratorKind Kind,
    IntegratorTier Tier,
    string Timezone,
    DateTime OccurredOn) : IDomainEvent;
