namespace Saasy.Catalog.Domain.ProductTypes;

public sealed class ProductType : AggregateRoot<ProductTypeId>
{
    public IntegratorId IntegratorId { get; private init; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private readonly List<Dimension> _dimensions = [];
    public IReadOnlyList<Dimension> Dimensions => _dimensions.AsReadOnly();

    private ProductType() { }

    public static ProductType Create(
        IntegratorId integratorId,
        string name,
        string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("ProductType name must not be empty.", nameof(name));

        var productType = new ProductType
        {
            Id = ProductTypeId.New(),
            IntegratorId = integratorId,
            Name = name,
            Description = description,
            Version = 0
        };

        productType.RaiseDomainEvent(new ProductTypeCreated(
            productType.Id,
            integratorId,
            name,
            description,
            DateTime.UtcNow));

        return productType;
    }

    public Dimension AddDimension(string code, string name, string unit, Aggregation aggregation)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Dimension code must not be empty.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Dimension name must not be empty.", nameof(name));

        if (_dimensions.Any(d => d.Code == code))
            throw new InvalidOperationException(
                $"A Dimension with code '{code}' already exists on ProductType {Id}.");

        var dimension = Dimension.Create(DimensionId.New(), code, name, unit, aggregation);
        _dimensions.Add(dimension);

        RaiseDomainEvent(new DimensionAdded(
            Id,
            dimension.Id,
            dimension.Code,
            dimension.Name,
            dimension.Unit,
            dimension.Aggregation,
            DateTime.UtcNow));

        return dimension;
    }
}
