namespace Saasy.Catalog.Domain.ProductTypes;

public readonly record struct IntegratorId(Guid Value)
{
    public static IntegratorId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
