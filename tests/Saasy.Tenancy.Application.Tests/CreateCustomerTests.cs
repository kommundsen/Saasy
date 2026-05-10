using Saasy.Tenancy.Application;
using Saasy.Tenancy.Application.Customers;
using Saasy.Tenancy.Domain.Customers;
using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Application.Tests;

public sealed class CreateCustomerTests
{
    [Fact]
    public async Task HandleAsync_WithValidCommand_ReturnsIdAndCommitsAndAddsToRepository()
    {
        var repo = new StubCustomerRepository();
        var uow = new StubUow();

        var handler = new CreateCustomer.Handler(repo, uow);
        var command = new CreateCustomer.Command(IntegratorId.New(), "cust-001", "Acme Corp");

        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, result.CustomerId.Value);
        Assert.True(uow.Committed);
        Assert.True(repo.Added);
    }
}

file sealed class StubCustomerRepository : ICustomerRepository
{
    public bool Added { get; private set; }
    private Customer? _stored;

    public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default)
        => Task.FromResult(_stored?.Id == id ? _stored : null);

    public void Add(Customer customer)
    {
        Added = true;
        _stored = customer;
    }
}

file sealed class StubUow : IUnitOfWork
{
    public bool Committed { get; private set; }

    public Task CommitAsync(CancellationToken ct = default)
    {
        Committed = true;
        return Task.CompletedTask;
    }
}
