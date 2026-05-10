using Saasy.Tenancy.Domain.Customers;
using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Domain.PropertyTests;

public class CustomerPropertyTests
{
    [Property]
    public void Create_WithNonEmptyExternalRef_Succeeds([From<NonEmptyPrintableStringStrategy>] string externalRef)
    {
        var customer = Customer.Create(
            integratorId: IntegratorId.New(),
            externalRef: externalRef,
            displayName: "Any Name");

        Assert.NotEqual(Guid.Empty, customer.Id.Value);
        Assert.Equal(externalRef, customer.ExternalRef);
    }

    [Property]
    public void Create_WithNonEmptyDisplayName_Succeeds([From<NonEmptyPrintableStringStrategy>] string displayName)
    {
        var customer = Customer.Create(
            integratorId: IntegratorId.New(),
            externalRef: "cust-001",
            displayName: displayName);

        Assert.Equal(displayName, customer.DisplayName);
    }

    [Property]
    public void Rename_WithAnyNonEmptyName_UpdatesDisplayName([From<NonEmptyPrintableStringStrategy>] string newName)
    {
        var customer = Customer.Create(
            integratorId: IntegratorId.New(),
            externalRef: "cust-001",
            displayName: "Original Name");

        customer.Rename(newName);

        Assert.Equal(newName, customer.DisplayName);
    }
}

internal sealed class NonEmptyPrintableStringStrategy : IStrategyProvider<string>
{
    public Strategy<string> Create()
        => Strategy.Strings(minLength: 1, maxLength: 200, minCodepoint: 33, maxCodepoint: 126);
}
