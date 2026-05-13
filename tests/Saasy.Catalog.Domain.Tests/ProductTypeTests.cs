namespace Saasy.Catalog.Domain.Tests;

public sealed class ProductTypeTests
{
    [Fact]
    public void AddDimension_DuplicateCode_Throws()
    {
        var productType = ProductType.Create(
            IntegratorId.New(),
            "Acme API Platform",
            "Products for the Acme API platform");

        productType.AddDimension("api_calls", "API Calls", "requests", Aggregation.Sum);

        Assert.Throws<InvalidOperationException>(() =>
            productType.AddDimension("api_calls", "API Calls (dup)", "requests", Aggregation.Sum));
    }
}
