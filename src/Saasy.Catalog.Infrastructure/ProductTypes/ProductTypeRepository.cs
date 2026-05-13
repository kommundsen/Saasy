using Microsoft.EntityFrameworkCore;
using Saasy.Catalog.Domain.ProductTypes;

namespace Saasy.Catalog.Infrastructure.ProductTypes;

internal sealed class ProductTypeRepository(CatalogDbContext db) : IProductTypeRepository
{
    public async Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken ct = default)
        => await db.ProductTypes.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<bool> ExistsAsync(ProductTypeId id, CancellationToken ct = default)
        => await db.ProductTypes.AnyAsync(x => x.Id == id, ct);

    public void Add(ProductType productType)
        => db.ProductTypes.Add(productType);
}
