namespace Saasy.Catalog.Domain.ProductTypes;

public readonly record struct DimensionId(Guid Value)
{
    public static DimensionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
