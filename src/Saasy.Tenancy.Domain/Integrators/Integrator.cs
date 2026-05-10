using Saasy.Tenancy.Domain;

namespace Saasy.Tenancy.Domain.Integrators;

public sealed class Integrator : AggregateRoot<IntegratorId>
{
    public string Name { get; private set; } = string.Empty;
    public IntegratorKind Kind { get; private init; }
    public IntegratorTier Tier { get; private set; }
    public Timezone Timezone { get; private set; } = null!;

    private readonly List<ApiKey> _apiKeys = [];
    public IReadOnlyList<ApiKey> ApiKeys => _apiKeys.AsReadOnly();

    private readonly List<IngestionCredential> _ingestionCredentials = [];
    public IReadOnlyList<IngestionCredential> IngestionCredentials => _ingestionCredentials.AsReadOnly();

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

    public (ApiKey ApiKey, string PlaintextSecret) MintApiKey(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("ApiKey name must not be empty.", nameof(name));

        var plaintextSecret = GeneratePlaintextSecret();
        var salt = ApiKeyHasher.GenerateSalt();
        var hashBytes = ApiKeyHasher.Hash(plaintextSecret, salt);

        var saltedHash = Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hashBytes);
        var last4 = plaintextSecret[^4..];
        var now = DateTime.UtcNow;

        var apiKey = ApiKey.Create(ApiKeyId.New(), name, saltedHash, last4, now);
        _apiKeys.Add(apiKey);

        RaiseDomainEvent(new ApiKeyMinted(Id, apiKey.Id, apiKey.Name, apiKey.Last4, now));

        return (apiKey, plaintextSecret);
    }

    public void RevokeApiKey(ApiKeyId apiKeyId)
    {
        var apiKey = _apiKeys.FirstOrDefault(k => k.Id == apiKeyId)
            ?? throw new InvalidOperationException($"ApiKey {apiKeyId} not found on Integrator {Id}.");

        if (apiKey.IsRevoked)
            return;

        var now = DateTime.UtcNow;
        apiKey.Revoke(now);

        RaiseDomainEvent(new ApiKeyRevoked(Id, apiKeyId, now));
    }

    // plaintextConnectionString is the real SAS connection string obtained from the
    // infrastructure layer (IIngestionCredentialMintService) before calling this method.
    // encrypt wraps it in ciphertext for persistence.
    // The plaintext is returned to the caller exactly once and must not be logged or persisted.
    public (IngestionCredential Credential, string PlaintextConnectionString) MintIngestionCredential(
        string plaintextConnectionString,
        Func<string, string> encrypt)
    {
        if (string.IsNullOrWhiteSpace(plaintextConnectionString))
            throw new ArgumentException(
                "Plaintext connection string must not be empty.", nameof(plaintextConnectionString));

        var encrypted = encrypt(plaintextConnectionString);
        var now = DateTime.UtcNow;

        var credential = IngestionCredential.Create(IngestionCredentialId.New(), encrypted, now);
        _ingestionCredentials.Add(credential);

        RaiseDomainEvent(new IngestionCredentialMinted(Id, credential.Id, now));

        return (credential, plaintextConnectionString);
    }

    private static string GeneratePlaintextSecret()
    {
        // 32 random bytes encoded as URL-safe Base64 without padding = 43 chars.
        var bytes = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
