namespace Saasy.Tenancy.Domain.Integrators;

public sealed class IngestionCredential
{
    public IngestionCredentialId Id { get; private init; }

    // The SAS connection string is stored encrypted at rest.
    // Plaintext is returned once at mint time and never persisted here.
    public string EncryptedConnectionString { get; private init; } = string.Empty;

    public DateTime CreatedAt { get; private init; }

    private IngestionCredential() { }

    internal static IngestionCredential Create(
        IngestionCredentialId id,
        string encryptedConnectionString,
        DateTime createdAt)
    {
        return new IngestionCredential
        {
            Id = id,
            EncryptedConnectionString = encryptedConnectionString,
            CreatedAt = createdAt
        };
    }
}
