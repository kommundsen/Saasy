using Saasy.Tenancy.Domain.Customers;
using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Domain.Tests;

public sealed class CustomerTests
{
    private static readonly IntegratorId SomeIntegratorId = IntegratorId.New();

    [Fact]
    public void Create_WithValidArguments_ReturnsCustomerWithNonEmptyId()
    {
        var customer = Customer.Create(
            integratorId: SomeIntegratorId,
            externalRef: "cust-001",
            displayName: "Acme Corp");

        Assert.NotEqual(Guid.Empty, customer.Id.Value);
        Assert.Equal(SomeIntegratorId, customer.IntegratorId);
        Assert.Equal("cust-001", customer.ExternalRef);
        Assert.Equal("Acme Corp", customer.DisplayName);
    }

    [Fact]
    public void Create_EmitsCustomerCreatedDomainEvent()
    {
        var customer = Customer.Create(
            integratorId: SomeIntegratorId,
            externalRef: "cust-001",
            displayName: "Acme Corp");

        var domainEvent = Assert.Single(customer.DomainEvents);
        var created = Assert.IsType<CustomerCreated>(domainEvent);
        Assert.Equal(customer.Id, created.CustomerId);
        Assert.Equal(SomeIntegratorId, created.IntegratorId);
        Assert.Equal("cust-001", created.ExternalRef);
        Assert.Equal("Acme Corp", created.DisplayName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithEmptyExternalRef_Throws(string? externalRef)
    {
        Assert.Throws<ArgumentException>(() =>
            Customer.Create(
                integratorId: SomeIntegratorId,
                externalRef: externalRef!,
                displayName: "Acme Corp"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithEmptyDisplayName_Throws(string? displayName)
    {
        Assert.Throws<ArgumentException>(() =>
            Customer.Create(
                integratorId: SomeIntegratorId,
                externalRef: "cust-001",
                displayName: displayName!));
    }

    [Fact]
    public void Rename_WithNewName_EmitsCustomerRenamedDomainEvent()
    {
        var customer = Customer.Create(
            integratorId: SomeIntegratorId,
            externalRef: "cust-001",
            displayName: "Acme Corp");

        customer.ClearDomainEvents();
        customer.Rename("Acme Inc");

        var domainEvent = Assert.Single(customer.DomainEvents);
        var renamed = Assert.IsType<CustomerRenamed>(domainEvent);
        Assert.Equal(customer.Id, renamed.CustomerId);
        Assert.Equal("Acme Inc", renamed.NewDisplayName);
        Assert.Equal("Acme Inc", customer.DisplayName);
    }

    [Fact]
    public void Rename_WithSameName_EmitsNoDomainEvent()
    {
        var customer = Customer.Create(
            integratorId: SomeIntegratorId,
            externalRef: "cust-001",
            displayName: "Acme Corp");

        customer.ClearDomainEvents();
        customer.Rename("Acme Corp");

        Assert.Empty(customer.DomainEvents);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Rename_WithEmptyName_Throws(string? newDisplayName)
    {
        var customer = Customer.Create(
            integratorId: SomeIntegratorId,
            externalRef: "cust-001",
            displayName: "Acme Corp");

        Assert.Throws<ArgumentException>(() => customer.Rename(newDisplayName!));
    }

}
