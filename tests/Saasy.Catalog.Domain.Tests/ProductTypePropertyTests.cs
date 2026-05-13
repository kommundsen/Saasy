namespace Saasy.Catalog.Domain.Tests;

public sealed class ProductTypePropertyTests
{
    // Property family: invariant + bound.
    //
    // For N distinct codes (N in [1, 20]):
    //   1. Adding each code once succeeds -- Dimensions count equals N after all adds.
    //   2. Adding any of those codes a second time throws InvalidOperationException.
    //
    // Anchored: count assertion pins magnitude; duplicate-throws assertion pins uniqueness.
    // Wrong impls caught: a no-op AddDimension, an AddDimension that drops the uniqueness check.
    [Property]
    public void AddDimension_NDistinctCodes_AllSucceedAndCountIsExact(
        [From<DimensionCountStrategy>] int n)
    {
        var productType = ProductType.Create(
            IntegratorId.New(),
            "Test Product Type",
            "Description");

        var codes = Enumerable.Range(1, n).Select(i => $"dim_{i}").ToList();

        foreach (var code in codes)
            productType.AddDimension(code, $"Name for {code}", "units", Aggregation.Sum);

        Assert.Equal(n, productType.Dimensions.Count);
    }

    [Property]
    public void AddDimension_DuplicateOfAnyAddedCode_AlwaysThrows(
        [From<DimensionCountStrategy>] int n)
    {
        var productType = ProductType.Create(
            IntegratorId.New(),
            "Test Product Type",
            "Description");

        var codes = Enumerable.Range(1, n).Select(i => $"dim_{i}").ToList();

        foreach (var code in codes)
            productType.AddDimension(code, $"Name for {code}", "units", Aggregation.Sum);

        // Adding the first code again must always throw.
        Assert.Throws<InvalidOperationException>(() =>
            productType.AddDimension(codes[0], "Repeat", "units", Aggregation.Last));
    }

    // Property family: invariant -- codes on a ProductType are always distinct after any sequence of valid adds.
    [Property]
    public void AddDimension_ResultingDimensions_HaveOnlyDistinctCodes(
        [From<DimensionCountStrategy>] int n)
    {
        var productType = ProductType.Create(
            IntegratorId.New(),
            "Test Product Type",
            "Description");

        var codes = Enumerable.Range(1, n).Select(i => $"dim_{i}").ToList();

        foreach (var code in codes)
            productType.AddDimension(code, $"Name for {code}", "units", Aggregation.Max);

        var distinctCount = productType.Dimensions.Select(d => d.Code).Distinct().Count();
        Assert.Equal(productType.Dimensions.Count, distinctCount);
    }

    // Property family: algebraic -- each Dimension returned by AddDimension carries the code
    // that was passed in (round-trip of code through the domain method).
    [Property]
    public void AddDimension_ReturnedDimension_PreservesCodeAndAggregation(
        [From<AggregationStrategy>] Aggregation aggregation)
    {
        var productType = ProductType.Create(
            IntegratorId.New(),
            "Test Product Type",
            "Description");

        var dimension = productType.AddDimension("api_calls", "API Calls", "requests", aggregation);

        Assert.Equal("api_calls", dimension.Code);
        Assert.Equal(aggregation, dimension.Aggregation);
    }
}

internal sealed class DimensionCountStrategy : IStrategyProvider<int>
{
    public Strategy<int> Create() => Strategy.Integers(min: 1, max: 20);
}

internal sealed class AggregationStrategy : IStrategyProvider<Aggregation>
{
    public Strategy<Aggregation> Create() => Strategy.Enums<Aggregation>();
}
