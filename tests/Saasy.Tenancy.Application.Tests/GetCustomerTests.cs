using Saasy.Tenancy.Application.Customers;
using Saasy.Tenancy.Domain.Customers;
using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Application.Tests;

public sealed class GetCustomerTests
{
    private static Customer CreateTestCustomer() => Customer.Create(
        integratorId: IntegratorId.New(),
        externalRef: "cust-001",
        displayName: "Acme Corp");

    [Fact]
    public async Task HandleAsync_WhenCustomerExists_ReturnsDto()
    {
        var customer = CreateTestCustomer();
        var repo = new StubGetCustomerRepository(customer);

        var handler = new GetCustomer.Handler(repo);
        var query = new GetCustomer.Query(customer.Id);

        var result = await handler.HandleAsync(query);

        Assert.NotNull(result);
        Assert.Equal(customer.Id, result.CustomerId);
        Assert.Equal(customer.IntegratorId, result.IntegratorId);
        Assert.Equal(customer.ExternalRef, result.ExternalRef);
        Assert.Equal(customer.DisplayName, result.DisplayName);
    }

    [Fact]
    public async Task HandleAsync_WhenCustomerNotFound_ReturnsNull()
    {
        var repo = new StubGetCustomerRepository(null);

        var handler = new GetCustomer.Handler(repo);
        var query = new GetCustomer.Query(CustomerId.New());

        var result = await handler.HandleAsync(query);

        Assert.Null(result);
    }
}

file sealed class StubGetCustomerRepository(Customer? customer) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default)
        => Task.FromResult(customer?.Id == id ? customer : null);

    public void Add(Customer c) { }
}
