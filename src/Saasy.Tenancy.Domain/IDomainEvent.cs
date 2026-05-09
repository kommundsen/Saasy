namespace Saasy.Tenancy.Domain;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
