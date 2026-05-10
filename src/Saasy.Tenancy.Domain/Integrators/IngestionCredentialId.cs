namespace Saasy.Tenancy.Domain.Integrators;

public readonly record struct IngestionCredentialId(Guid Value)
{
    public static IngestionCredentialId New() => new(Guid.NewGuid());
}
