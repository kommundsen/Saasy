namespace Saasy.Tenancy.Domain.Customers;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default);
    void Add(Customer customer);
}
