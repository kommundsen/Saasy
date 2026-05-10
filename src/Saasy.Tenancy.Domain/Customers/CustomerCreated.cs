using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Domain.Customers;

public sealed record CustomerCreated(
    CustomerId CustomerId,
    IntegratorId IntegratorId,
    string ExternalRef,
    string DisplayName,
    DateTime OccurredOn) : IDomainEvent;
