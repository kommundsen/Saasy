using Conjecture.Core;
using Conjecture.Xunit.V3;
using Saasy.Tenancy.Domain.Integrators;
using Xunit;

namespace Saasy.Tenancy.Domain.PropertyTests;

public class ApiKeyHashingPropertyTests
{
    [Property]
    public void Hash_SameSecretAndSalt_ProducesSameResult(
        [From<NonEmptyAsciiStringStrategy>] string secret,
        [From<FixedSaltStrategy>] byte[] salt)
    {
        var first = ApiKeyHasher.Hash(secret, salt);
        var second = ApiKeyHasher.Hash(secret, salt);
        Assert.Equal(first, second);
    }

    [Property]
    public void Hash_SameSecret_DifferentSalts_ProducesDifferentResults(
        [From<NonEmptyAsciiStringStrategy>] string secret,
        [From<FixedSaltStrategy>] byte[] salt1,
        [From<FixedSaltStrategy>] byte[] salt2)
    {
        Assume.That(!salt1.SequenceEqual(salt2));

        var hash1 = ApiKeyHasher.Hash(secret, salt1);
        var hash2 = ApiKeyHasher.Hash(secret, salt2);
        Assert.NotEqual(hash1, hash2);
    }

    [Property]
    public void Verify_ReturnsFalse_ForAnyNonMatchingSecret(
        [From<NonEmptyAsciiStringStrategy>] string storedSecret,
        [From<NonEmptyAsciiStringStrategy>] string candidateSecret)
    {
        Assume.That(storedSecret != candidateSecret);

        var salt = ApiKeyHasher.GenerateSalt();
        var hashBytes = ApiKeyHasher.Hash(storedSecret, salt);
        var storedValue = Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hashBytes);

        var result = ApiKeyHasher.Verify(candidateSecret, storedValue);

        Assert.False(result);
    }

    [Property]
    public void Verify_ReturnsTrue_ForMatchingSecret(
        [From<NonEmptyAsciiStringStrategy>] string secret)
    {
        var salt = ApiKeyHasher.GenerateSalt();
        var hashBytes = ApiKeyHasher.Hash(secret, salt);
        var storedValue = Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hashBytes);

        var result = ApiKeyHasher.Verify(secret, storedValue);

        Assert.True(result);
    }
}

internal sealed class NonEmptyAsciiStringStrategy : IStrategyProvider<string>
{
    // ASCII printable range: codepoints 33-126 (excludes space and control chars)
    public Strategy<string> Create()
        => Strategy.Strings(minLength: 1, maxLength: 128, minCodepoint: 33, maxCodepoint: 126);
}

internal sealed class FixedSaltStrategy : IStrategyProvider<byte[]>
{
    public Strategy<byte[]> Create()
        => Strategy.Arrays(Strategy.Integers<byte>(), minSize: 16, maxSize: 16);
}
