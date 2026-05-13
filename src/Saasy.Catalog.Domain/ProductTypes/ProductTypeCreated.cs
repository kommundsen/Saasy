namespace Saasy.Catalog.Domain.ProductTypes;

public sealed record ProductTypeCreated(
    ProductTypeId ProductTypeId,
    IntegratorId IntegratorId,
    string Name,
    string Description,
    DateTime OccurredOn) : IDomainEvent;
