using Saasy.Tenancy.Domain;

namespace Saasy.Tenancy.Domain.Integrators;

public sealed class Integrator : AggregateRoot<IntegratorId>
{
    public string Name { get; private set; } = string.Empty;
    public IntegratorKind Kind { get; private init; }
    public IntegratorTier Tier { get; private set; }
    public Timezone Timezone { get; private set; } = null!;

    private Integrator() { }

    public static Integrator Create(
        string name,
        IntegratorKind kind,
        IntegratorTier tier,
        Timezone timezone)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Integrator name must not be empty.", nameof(name));

        var integrator = new Integrator
        {
            Id = IntegratorId.New(),
            Name = name,
            Kind = kind,
            Tier = tier,
            Timezone = timezone,
            Version = 0
        };

        integrator.RaiseDomainEvent(new IntegratorCreated(
            integrator.Id,
            integrator.Name,
            integrator.Kind,
            integrator.Tier,
            integrator.Timezone.IanaName,
            DateTime.UtcNow));

        return integrator;
    }

    public void ChangeTier(IntegratorTier newTier)
    {
        if (Tier == newTier)
            return;

        var oldTier = Tier;
        Tier = newTier;

        RaiseDomainEvent(new IntegratorTierChanged(Id, oldTier, newTier, DateTime.UtcNow));
    }
}
