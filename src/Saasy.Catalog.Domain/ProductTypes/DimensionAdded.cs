namespace Saasy.Catalog.Domain.ProductTypes;

public sealed record DimensionAdded(
    ProductTypeId ProductTypeId,
    DimensionId DimensionId,
    string Code,
    string Name,
    string Unit,
    Aggregation Aggregation,
    DateTime OccurredOn) : IDomainEvent;
