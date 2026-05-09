namespace Saasy.Tenancy.Domain.Integrators;

public readonly record struct IntegratorId(Guid Value)
{
    public static IntegratorId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
