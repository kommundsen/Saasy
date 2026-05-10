using Saasy.Tenancy.Application.Customers;
using Saasy.Tenancy.Domain.Customers;
using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Application.Tests;

public sealed class RenameCustomerTests
{
    private static Customer CreateTestCustomer() => Customer.Create(
        integratorId: IntegratorId.New(),
        externalRef: "cust-001",
        displayName: "Acme Corp");

    [Fact]
    public async Task HandleAsync_WhenCustomerExists_ReturnsTrue()
    {
        var customer = CreateTestCustomer();
        var repo = new StubRenameRepository(customer);
        var uow = new StubRenameUow();

        var handler = new RenameCustomer.Handler(repo, uow);
        var command = new RenameCustomer.Command(customer.Id, "Acme Inc");

        var found = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.True(found);
    }

    [Fact]
    public async Task HandleAsync_WhenCustomerExists_UpdatesDisplayName()
    {
        var customer = CreateTestCustomer();
        var repo = new StubRenameRepository(customer);
        var uow = new StubRenameUow();

        var handler = new RenameCustomer.Handler(repo, uow);
        var command = new RenameCustomer.Command(customer.Id, "Acme Inc");

        await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.Equal("Acme Inc", customer.DisplayName);
    }

    [Fact]
    public async Task HandleAsync_WhenCustomerExists_CommitsUnitOfWork()
    {
        var customer = CreateTestCustomer();
        var repo = new StubRenameRepository(customer);
        var uow = new StubRenameUow();

        var handler = new RenameCustomer.Handler(repo, uow);
        var command = new RenameCustomer.Command(customer.Id, "Acme Inc");

        await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.True(uow.Committed);
    }

    [Fact]
    public async Task HandleAsync_WhenCustomerNotFound_ReturnsFalse()
    {
        var repo = new StubRenameRepository(null);
        var uow = new StubRenameUow();

        var handler = new RenameCustomer.Handler(repo, uow);
        var command = new RenameCustomer.Command(CustomerId.New(), "Acme Inc");

        var found = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.False(found);
    }
}

file sealed class StubRenameRepository(Customer? customer) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default)
        => Task.FromResult(customer?.Id == id ? customer : null);

    public void Add(Customer c) { }
}

file sealed class StubRenameUow : IUnitOfWork
{
    public bool Committed { get; private set; }

    public Task CommitAsync(CancellationToken ct = default)
    {
        Committed = true;
        return Task.CompletedTask;
    }
}
