using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Domain.Customers;

public sealed class Customer : AggregateRoot<CustomerId>
{
    public IntegratorId IntegratorId { get; private init; }
    public string ExternalRef { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private init; }

    private Customer() { }

    public static Customer Create(
        IntegratorId integratorId,
        string externalRef,
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(externalRef))
            throw new ArgumentException("ExternalRef must not be empty.", nameof(externalRef));

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("DisplayName must not be empty.", nameof(displayName));

        var now = DateTime.UtcNow;

        var customer = new Customer
        {
            Id = CustomerId.New(),
            IntegratorId = integratorId,
            ExternalRef = externalRef,
            DisplayName = displayName,
            CreatedAt = now,
            Version = 0
        };

        customer.RaiseDomainEvent(new CustomerCreated(
            customer.Id,
            customer.IntegratorId,
            customer.ExternalRef,
            customer.DisplayName,
            now));

        return customer;
    }

    public void Rename(string newDisplayName)
    {
        if (string.IsNullOrWhiteSpace(newDisplayName))
            throw new ArgumentException("DisplayName must not be empty.", nameof(newDisplayName));

        if (DisplayName == newDisplayName)
            return;

        DisplayName = newDisplayName;
        RaiseDomainEvent(new CustomerRenamed(Id, IntegratorId, newDisplayName, DateTime.UtcNow));
    }
}
