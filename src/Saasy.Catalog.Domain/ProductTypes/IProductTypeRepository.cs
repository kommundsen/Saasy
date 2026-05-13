namespace Saasy.Catalog.Domain.ProductTypes;

public interface IProductTypeRepository
{
    Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken ct = default);
    Task<bool> ExistsAsync(ProductTypeId id, CancellationToken ct = default);
    void Add(ProductType productType);
}
