namespace Saasy.Catalog.Domain;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
