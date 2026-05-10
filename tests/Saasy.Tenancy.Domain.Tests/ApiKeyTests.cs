using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Domain.Tests;

public sealed class ApiKeyTests
{
    private static Integrator CreateIntegrator() => Integrator.Create(
        name: "Acme Corp",
        kind: IntegratorKind.Production,
        tier: IntegratorTier.Free,
        timezone: Timezone.Create("UTC"));

    [Fact]
    public void MintApiKey_ReturnsApiKeyEntityAndNonEmptyPlaintextSecret()
    {
        var integrator = CreateIntegrator();

        var (apiKey, plaintext) = integrator.MintApiKey("test-key");

        Assert.NotNull(apiKey);
        Assert.False(string.IsNullOrEmpty(plaintext));
    }

    [Fact]
    public void MintApiKey_PlaintextNotStoredOnApiKey()
    {
        var integrator = CreateIntegrator();

        var (apiKey, plaintext) = integrator.MintApiKey("test-key");

        Assert.NotEqual(plaintext, apiKey.HashedSecret);
        Assert.DoesNotContain(plaintext, apiKey.HashedSecret);
    }

    [Fact]
    public void MintApiKey_StoredHashContainsSaltedFormat()
    {
        var integrator = CreateIntegrator();

        var (apiKey, _) = integrator.MintApiKey("test-key");

        // HashedSecret is stored as "base64salt.base64hash"
        Assert.Contains(".", apiKey.HashedSecret);
    }

    [Fact]
    public void MintApiKey_ApiKeyIsAddedToIntegrator()
    {
        var integrator = CreateIntegrator();

        var (apiKey, _) = integrator.MintApiKey("test-key");

        Assert.Contains(integrator.ApiKeys, k => k.Id == apiKey.Id);
    }

    [Fact]
    public void MintApiKey_EmitsApiKeyMintedDomainEvent()
    {
        var integrator = CreateIntegrator();
        integrator.ClearDomainEvents();

        var (apiKey, _) = integrator.MintApiKey("test-key");

        var domainEvent = Assert.Single(integrator.DomainEvents);
        var minted = Assert.IsType<ApiKeyMinted>(domainEvent);
        Assert.Equal(integrator.Id, minted.IntegratorId);
        Assert.Equal(apiKey.Id, minted.ApiKeyId);
        Assert.Equal("test-key", minted.Name);
        Assert.Equal(apiKey.Last4, minted.Last4);
    }

    [Fact]
    public void RevokeApiKey_SetsRevokedAt()
    {
        var integrator = CreateIntegrator();
        var (apiKey, _) = integrator.MintApiKey("test-key");

        integrator.RevokeApiKey(apiKey.Id);

        Assert.True(apiKey.IsRevoked);
        Assert.NotNull(apiKey.RevokedAt);
    }

    [Fact]
    public void RevokeApiKey_EmitsApiKeyRevokedDomainEvent()
    {
        var integrator = CreateIntegrator();
        var (apiKey, _) = integrator.MintApiKey("test-key");
        integrator.ClearDomainEvents();

        integrator.RevokeApiKey(apiKey.Id);

        var domainEvent = Assert.Single(integrator.DomainEvents);
        var revoked = Assert.IsType<ApiKeyRevoked>(domainEvent);
        Assert.Equal(integrator.Id, revoked.IntegratorId);
        Assert.Equal(apiKey.Id, revoked.ApiKeyId);
    }

    [Fact]
    public void RevokeApiKey_WhenAlreadyRevoked_EmitsNoDomainEvent()
    {
        var integrator = CreateIntegrator();
        var (apiKey, _) = integrator.MintApiKey("test-key");
        integrator.RevokeApiKey(apiKey.Id);
        integrator.ClearDomainEvents();

        integrator.RevokeApiKey(apiKey.Id);

        Assert.Empty(integrator.DomainEvents);
    }

    [Fact]
    public void RevokeApiKey_WithUnknownId_Throws()
    {
        var integrator = CreateIntegrator();

        Assert.Throws<InvalidOperationException>(
            () => integrator.RevokeApiKey(ApiKeyId.New()));
    }

    [Fact]
    public void MintedApiKey_IsNotRevoked()
    {
        var integrator = CreateIntegrator();
        var (apiKey, _) = integrator.MintApiKey("test-key");

        Assert.False(apiKey.IsRevoked);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void MintApiKey_WithInvalidName_Throws(string? name)
    {
        var integrator = CreateIntegrator();

        Assert.Throws<ArgumentException>(
            () => integrator.MintApiKey(name!));
    }
}
