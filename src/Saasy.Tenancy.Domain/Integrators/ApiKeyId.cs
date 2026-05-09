namespace Saasy.Tenancy.Domain.Integrators;

public readonly record struct ApiKeyId(Guid Value)
{
    public static ApiKeyId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
