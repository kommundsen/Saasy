namespace Saasy.Tenancy.Domain.Tests;

public class ApiKeyPropertyTests
{
    private static Integrator CreateIntegrator() => Integrator.Create(
        name: "Acme Corp",
        kind: IntegratorKind.Production,
        tier: IntegratorTier.Free,
        timezone: Timezone.Create("UTC"));

    // Promotion of ApiKeyTests.MintApiKey_Last4MatchesTailOfPlaintext
    //            and ApiKeyTests.MintApiKey_TwoCallsReturnDifferentPlaintexts.
    //
    // For N successive mints (N in [2, 50]):
    //   1. Every minted key's Last4 equals the last four characters of its plaintext.
    //   2. All N plaintexts are distinct (the generator uses RandomNumberGenerator internally).
    //
    // Folded into one property because both invariants share the same mint loop; asserting
    // both reads cleanly and avoids running the loop twice.
    [Property]
    public void MintApiKey_Last4InvariantAndUniquePlaintexts(
        [From<MintCountStrategy>] int n)
    {
        var integrator = CreateIntegrator();
        var results = new List<(string Plaintext, string Last4)>(n);

        for (int i = 0; i < n; i++)
        {
            var (apiKey, plaintext) = integrator.MintApiKey($"key-{i}");
            results.Add((plaintext, apiKey.Last4));
        }

        foreach (var (plaintext, last4) in results)
            Assert.Equal(plaintext.Substring(plaintext.Length - 4), last4);

        var distinctPlaintexts = results.Select(r => r.Plaintext).Distinct().Count();
        Assert.Equal(n, distinctPlaintexts);
    }
}

internal sealed class MintCountStrategy : IStrategyProvider<int>
{
    public Strategy<int> Create() => Strategy.Integers(min: 2, max: 50);
}
