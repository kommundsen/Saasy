using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Domain.Tests;

public sealed class IngestionCredentialTests
{
    private static Integrator CreateIntegrator() => Integrator.Create(
        name: "Acme Corp",
        kind: IntegratorKind.Production,
        tier: IntegratorTier.Free,
        timezone: Timezone.Create("UTC"));

    private static string FakeEncrypt(string plaintext) => "enc:" + plaintext;

    private const string SomePlaintext =
        "Endpoint=sb://saasy.servicebus.windows.net/;SharedAccessKeyName=SendOnly;SharedAccessKey=AAAA";

    [Fact]
    public void MintIngestionCredential_ReturnsPlaintextUnchanged()
    {
        var integrator = CreateIntegrator();

        var (_, returned) = integrator.MintIngestionCredential(SomePlaintext, FakeEncrypt);

        Assert.Equal(SomePlaintext, returned);
    }

    [Fact]
    public void MintIngestionCredential_StoredFormIsNotPlaintext()
    {
        var integrator = CreateIntegrator();

        var (credential, plaintext) = integrator.MintIngestionCredential(SomePlaintext, FakeEncrypt);

        Assert.NotEqual(plaintext, credential.EncryptedConnectionString);
    }

    [Fact]
    public void MintIngestionCredential_PlaintextNotContainedInStoredForm_WhenEncryptorObfuscates()
    {
        var integrator = CreateIntegrator();
        var (credential, plaintext) = integrator.MintIngestionCredential(
            SomePlaintext,
            _ => "OPAQUE_CIPHERTEXT_NO_RELATION_TO_INPUT");

        Assert.DoesNotContain(plaintext, credential.EncryptedConnectionString);
    }

    [Fact]
    public void MintIngestionCredential_AddsCredentialToIntegrator()
    {
        var integrator = CreateIntegrator();

        var (credential, _) = integrator.MintIngestionCredential(SomePlaintext, FakeEncrypt);

        Assert.Contains(integrator.IngestionCredentials, c => c.Id == credential.Id);
    }

    [Fact]
    public void MintIngestionCredential_EmitsIngestionCredentialMintedDomainEvent()
    {
        var integrator = CreateIntegrator();
        integrator.ClearDomainEvents();

        var (credential, _) = integrator.MintIngestionCredential(SomePlaintext, FakeEncrypt);

        var domainEvent = Assert.Single(integrator.DomainEvents);
        var minted = Assert.IsType<IngestionCredentialMinted>(domainEvent);
        Assert.Equal(integrator.Id, minted.IntegratorId);
        Assert.Equal(credential.Id, minted.IngestionCredentialId);
    }

    [Fact]
    public void MintIngestionCredential_TwoCallsWithDifferentPlaintextsProduceDifferentStoredForms()
    {
        var integrator = CreateIntegrator();

        var (credential1, _) = integrator.MintIngestionCredential(
            "Endpoint=sb://saasy.servicebus.windows.net/;SharedAccessKeyName=SendOnly;SharedAccessKey=AAAA",
            FakeEncrypt);
        var (credential2, _) = integrator.MintIngestionCredential(
            "Endpoint=sb://saasy.servicebus.windows.net/;SharedAccessKeyName=SendOnly;SharedAccessKey=BBBB",
            FakeEncrypt);

        Assert.NotEqual(
            credential1.EncryptedConnectionString,
            credential2.EncryptedConnectionString);
    }

    [Fact]
    public void MintIngestionCredential_EmptyPlaintext_Throws()
    {
        var integrator = CreateIntegrator();

        Assert.Throws<ArgumentException>(
            () => integrator.MintIngestionCredential(string.Empty, FakeEncrypt));
    }
}
