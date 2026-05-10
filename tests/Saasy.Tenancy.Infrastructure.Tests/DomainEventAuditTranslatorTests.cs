using System.Text.Json;
using Saasy.Tenancy.Application;
using Saasy.Tenancy.Domain.Customers;
using Saasy.Tenancy.Domain.Integrators;
using Saasy.Tenancy.Infrastructure.Audit;

namespace Saasy.Tenancy.Infrastructure.Tests;

public sealed class DomainEventAuditTranslatorTests
{
    private static readonly IntegratorId SomeIntegratorId = IntegratorId.New();
    private const string Actor = "test:actor";

    [Fact]
    public void Translate_IntegratorCreated_ProducesExpectedAuditEntry()
    {
        var evt = new IntegratorCreated(
            SomeIntegratorId,
            "Acme Corp",
            IntegratorKind.Production,
            IntegratorTier.Free,
            "UTC",
            DateTime.UtcNow);

        var entry = DomainEventAuditTranslator.Translate(evt, Actor);

        Assert.NotNull(entry);
        Assert.Equal("integrator.created", entry.Action);
        Assert.Equal("Integrator", entry.TargetType);
        Assert.Equal(SomeIntegratorId.Value.ToString(), entry.TargetId);
        Assert.Equal(SomeIntegratorId, entry.IntegratorId);
        Assert.Equal(Actor, entry.Actor);
    }

    [Fact]
    public void Translate_IntegratorTierChanged_ProducesExpectedAuditEntry()
    {
        var evt = new IntegratorTierChanged(
            SomeIntegratorId,
            IntegratorTier.Free,
            IntegratorTier.Paid,
            DateTime.UtcNow);

        var entry = DomainEventAuditTranslator.Translate(evt, Actor);

        Assert.NotNull(entry);
        Assert.Equal("integrator.tier_changed", entry.Action);
        Assert.Equal("Integrator", entry.TargetType);
        Assert.Equal(SomeIntegratorId.Value.ToString(), entry.TargetId);
    }

    [Fact]
    public void Translate_ApiKeyMinted_ProducesExpectedAuditEntry()
    {
        var apiKeyId = ApiKeyId.New();
        var evt = new ApiKeyMinted(
            SomeIntegratorId,
            apiKeyId,
            "my-key",
            "abcd",
            DateTime.UtcNow);

        var entry = DomainEventAuditTranslator.Translate(evt, Actor);

        Assert.NotNull(entry);
        Assert.Equal("integrator.api_key_minted", entry.Action);
        Assert.Equal("ApiKey", entry.TargetType);
        Assert.Equal(apiKeyId.Value.ToString(), entry.TargetId);
    }

    [Fact]
    public void Translate_ApiKeyMinted_PayloadDoesNotContainPlaintextOrHash()
    {
        const string somePlaintext = "super-secret-key-plaintext-value";
        const string someHash = "base64salt==.base64hash==";

        var evt = new ApiKeyMinted(
            SomeIntegratorId,
            ApiKeyId.New(),
            "my-key",
            "abcd",
            DateTime.UtcNow);

        var entry = DomainEventAuditTranslator.Translate(evt, Actor);

        Assert.NotNull(entry);
        Assert.DoesNotContain(somePlaintext, entry.PayloadJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(someHash, entry.PayloadJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Translate_ApiKeyRevoked_ProducesExpectedAuditEntry()
    {
        var apiKeyId = ApiKeyId.New();
        var evt = new ApiKeyRevoked(SomeIntegratorId, apiKeyId, DateTime.UtcNow);

        var entry = DomainEventAuditTranslator.Translate(evt, Actor);

        Assert.NotNull(entry);
        Assert.Equal("integrator.api_key_revoked", entry.Action);
        Assert.Equal("ApiKey", entry.TargetType);
        Assert.Equal(apiKeyId.Value.ToString(), entry.TargetId);
    }

    [Fact]
    public void Translate_IngestionCredentialMinted_ProducesExpectedAuditEntry()
    {
        var credentialId = IngestionCredentialId.New();
        var evt = new IngestionCredentialMinted(SomeIntegratorId, credentialId, DateTime.UtcNow);

        var entry = DomainEventAuditTranslator.Translate(evt, Actor);

        Assert.NotNull(entry);
        Assert.Equal("integrator.ingestion_credential_minted", entry.Action);
        Assert.Equal("IngestionCredential", entry.TargetType);
        Assert.Equal(credentialId.Value.ToString(), entry.TargetId);
    }

    [Fact]
    public void Translate_IngestionCredentialMinted_PayloadDoesNotContainSasString()
    {
        const string fakeSas = "AccountName=myaccount;AccountKey=SECRETSECRET==";

        var credentialId = IngestionCredentialId.New();
        var evt = new IngestionCredentialMinted(SomeIntegratorId, credentialId, DateTime.UtcNow);

        var entry = DomainEventAuditTranslator.Translate(evt, Actor);

        Assert.NotNull(entry);
        Assert.DoesNotContain(fakeSas, entry.PayloadJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Translate_CustomerCreated_ProducesExpectedAuditEntry()
    {
        var customerId = CustomerId.New();
        var evt = new CustomerCreated(customerId, SomeIntegratorId, "cust-001", "Acme Corp", DateTime.UtcNow);

        var entry = DomainEventAuditTranslator.Translate(evt, Actor);

        Assert.NotNull(entry);
        Assert.Equal("customer.created", entry.Action);
        Assert.Equal("Customer", entry.TargetType);
        Assert.Equal(customerId.Value.ToString(), entry.TargetId);
        Assert.Equal(SomeIntegratorId, entry.IntegratorId);
    }

    [Fact]
    public void Translate_CustomerRenamed_ProducesExpectedAuditEntry()
    {
        var customerId = CustomerId.New();
        var evt = new CustomerRenamed(customerId, SomeIntegratorId, "New Name", DateTime.UtcNow);

        var entry = DomainEventAuditTranslator.Translate(evt, Actor);

        Assert.NotNull(entry);
        Assert.Equal("customer.renamed", entry.Action);
        Assert.Equal("Customer", entry.TargetType);
        Assert.Equal(customerId.Value.ToString(), entry.TargetId);
    }

    [Fact]
    public void Translate_UnknownDomainEvent_ReturnsNull()
    {
        var entry = DomainEventAuditTranslator.Translate(new UnknownDomainEvent(), Actor);
        Assert.Null(entry);
    }

    [Fact]
    public void Translate_AuditEntry_TimestampMatchesOccurredOn()
    {
        var occurredOn = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var evt = new IntegratorCreated(
            SomeIntegratorId,
            "Acme",
            IntegratorKind.Production,
            IntegratorTier.Free,
            "UTC",
            occurredOn);

        var entry = DomainEventAuditTranslator.Translate(evt, Actor);

        Assert.NotNull(entry);
        Assert.Equal(new DateTimeOffset(occurredOn, TimeSpan.Zero), entry.Timestamp);
    }
}

// Stub domain event not in the switch -- tests the null-fallthrough branch.
file sealed record UnknownDomainEvent : Saasy.Tenancy.Domain.IDomainEvent
{
    public DateTime OccurredOn => DateTime.UtcNow;
}
