namespace Saasy.Tenancy.Domain.Integrators;

public sealed class ApiKey
{
    public ApiKeyId Id { get; private init; }
    public string Name { get; private init; } = string.Empty;
    public string HashedSecret { get; private init; } = string.Empty;
    public string Last4 { get; private init; } = string.Empty;
    public DateTime CreatedAt { get; private init; }
    public DateTime? RevokedAt { get; private set; }

    public bool IsRevoked => RevokedAt.HasValue;

    private ApiKey() { }

    internal static ApiKey Create(
        ApiKeyId id,
        string name,
        string hashedSecret,
        string last4,
        DateTime createdAt)
    {
        return new ApiKey
        {
            Id = id,
            Name = name,
            HashedSecret = hashedSecret,
            Last4 = last4,
            CreatedAt = createdAt
        };
    }

    internal void Revoke(DateTime revokedAt)
    {
        if (IsRevoked)
            return;
        RevokedAt = revokedAt;
    }
}
