using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Domain.Customers;

public sealed record CustomerRenamed(
    CustomerId CustomerId,
    IntegratorId IntegratorId,
    string NewDisplayName,
    DateTime OccurredOn) : IDomainEvent;
