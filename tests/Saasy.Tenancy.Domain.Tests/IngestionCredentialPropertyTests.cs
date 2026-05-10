using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Domain.Tests;

public class IngestionCredentialPropertyTests
{
    // For any non-empty plaintext, the stored form must never equal the plaintext
    // when the encryptor produces a distinct output.
    [Property]
    public void MintIngestionCredential_StoredFormNeverEqualsPlaintext(
        [From<NonEmptyPrintableStringStrategy>] string plaintext)
    {
        var integrator = Integrator.Create(
            name: "Acme",
            kind: IntegratorKind.Production,
            tier: IntegratorTier.Free,
            timezone: Timezone.Create("UTC"));

        var (credential, returned) = integrator.MintIngestionCredential(
            plaintext,
            p => "CIPHERTEXT_IS_OPAQUE:" + p.GetHashCode().ToString("X"));

        Assert.NotEqual(returned, credential.EncryptedConnectionString);
    }

    // For any non-empty plaintext, the method returns that exact plaintext to the caller.
    [Property]
    public void MintIngestionCredential_ReturnsExactPlaintextPassedIn(
        [From<NonEmptyPrintableStringStrategy>] string plaintext)
    {
        var integrator = Integrator.Create(
            name: "Acme",
            kind: IntegratorKind.Production,
            tier: IntegratorTier.Free,
            timezone: Timezone.Create("UTC"));

        var (_, returned) = integrator.MintIngestionCredential(plaintext, p => "enc:" + p);

        Assert.Equal(plaintext, returned);
    }

    // Two calls with different plaintexts must produce different stored forms
    // when using the same deterministic encryptor -- the encryptor input differs.
    [Property]
    public void MintIngestionCredential_DifferentPlaintextsYieldDifferentStoredForms(
        [From<NonEmptyPrintableStringStrategy>] string plaintextA,
        [From<NonEmptyPrintableStringStrategy>] string plaintextB)
    {
        if (plaintextA == plaintextB)
            return; // trivially equal inputs; skip this case

        var integrator = Integrator.Create(
            name: "Acme",
            kind: IntegratorKind.Production,
            tier: IntegratorTier.Free,
            timezone: Timezone.Create("UTC"));

        var (credential1, _) = integrator.MintIngestionCredential(plaintextA, p => "enc:" + p);
        var (credential2, _) = integrator.MintIngestionCredential(plaintextB, p => "enc:" + p);

        Assert.NotEqual(
            credential1.EncryptedConnectionString,
            credential2.EncryptedConnectionString);
    }
}
