namespace Saasy.Catalog.Domain.ProductTypes;

public readonly record struct ProductTypeId(Guid Value)
{
    public static ProductTypeId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
