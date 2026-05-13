namespace Saasy.Catalog.Domain.ProductTypes;

public sealed class Dimension
{
    public DimensionId Id { get; private init; }
    public string Code { get; private init; } = string.Empty;
    public string Name { get; private init; } = string.Empty;
    public string Unit { get; private init; } = string.Empty;
    public Aggregation Aggregation { get; private init; }

    private Dimension() { }

    internal static Dimension Create(
        DimensionId id,
        string code,
        string name,
        string unit,
        Aggregation aggregation)
    {
        return new Dimension
        {
            Id = id,
            Code = code,
            Name = name,
            Unit = unit,
            Aggregation = aggregation
        };
    }
}
