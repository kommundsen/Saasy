using Microsoft.EntityFrameworkCore;
using Saasy.Tenancy.Domain.Customers;

namespace Saasy.Tenancy.Infrastructure.Customers;

internal sealed class CustomerRepository(TenancyDbContext db) : ICustomerRepository
{
    public async Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default)
        => await db.Customers.FirstOrDefaultAsync(x => x.Id == id, ct);

    public void Add(Customer customer)
        => db.Customers.Add(customer);
}
