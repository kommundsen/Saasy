using Saasy.Metering.Infrastructure.Ingestion;

namespace Saasy.Metering.Infrastructure.Tests;

public sealed class IngestRateLimiterTests
{
    private const int Cap = 10;

    // Anchor: no window entry yet -> worst-case 60-second guidance.
    [Fact]
    public void SecondsUntilWindowReset_NoEntry_Returns60()
    {
        var limiter = new IngestRateLimiter();

        // A fresh limiter with no window entry returns the full window duration.
        Assert.Equal(60, limiter.SecondsUntilWindowReset(Guid.NewGuid()));
    }

    // Property A: for any n in [0, cap-1], consuming n times returns true for each
    // call; after exactly cap consumptions the next call returns false.
    // The property tests both "within-cap -> true" (n calls) and "exceed-cap -> false"
    // (the cap+1 call) in a single generative sweep.
    [Property]
    public void TryConsume_WithinCapAllTrueExceedCapFalse(
        [From<ConsumeCountStrategy>] int n,
        [From<IntegratorSeedStrategy>] int integratorSeed)
    {
        var limiter = new IngestRateLimiter();
        var integrator = IntegratorFromSeed(integratorSeed);

        // Consume n times (n in [0, cap-1]) -- all must succeed.
        for (var i = 0; i < n; i++)
            Assert.True(limiter.TryConsume(integrator, capPerMinute: Cap));

        // Exhaust the remainder of the cap.
        for (var i = n; i < Cap; i++)
            limiter.TryConsume(integrator, capPerMinute: Cap);

        // The next call after cap consumptions must be rejected.
        Assert.False(limiter.TryConsume(integrator, capPerMinute: Cap));
    }

    // Property B: for any pair of distinct integrator IDs, consuming on one does
    // not affect the other's counter. Both can consume up to cap - 1 independently.
    [Property]
    public void TryConsume_DistinctIntegrators_CountsAreIndependent(
        [From<IndependentConsumeStrategy>] IndependentConsumePair pair)
    {
        var limiter = new IngestRateLimiter();

        for (var i = 0; i < pair.CountA; i++)
            Assert.True(limiter.TryConsume(pair.IntegratorA, capPerMinute: Cap));

        for (var i = 0; i < pair.CountB; i++)
            Assert.True(limiter.TryConsume(pair.IntegratorB, capPerMinute: Cap));
    }

    // Property C: after any number of consumes n in [1, cap], SecondsUntilWindowReset
    // returns a value in [1, 60].
    [Property]
    public void SecondsUntilWindowReset_AfterAnyConsume_IsInBounds(
        [From<PositiveConsumeCountStrategy>] int n,
        [From<IntegratorSeedStrategy>] int integratorSeed)
    {
        var limiter = new IngestRateLimiter();
        var integrator = IntegratorFromSeed(integratorSeed);

        for (var i = 0; i < n; i++)
            limiter.TryConsume(integrator, capPerMinute: Cap);

        var remaining = limiter.SecondsUntilWindowReset(integrator);
        Assert.InRange(remaining, 1, 60);
    }

    private static Guid IntegratorFromSeed(int seed)
        => new(seed, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}

// Generates n in [0, cap-1] -- within the cap, for the partial-consume prefix.
internal sealed class ConsumeCountStrategy : IStrategyProvider<int>
{
    public Strategy<int> Create()
        => Strategy.Integers<int>(0, IngestRateLimiterTests_Cap.Value - 1);
}

// Generates n in [1, cap] for the bounds property.
internal sealed class PositiveConsumeCountStrategy : IStrategyProvider<int>
{
    public Strategy<int> Create()
        => Strategy.Integers<int>(1, IngestRateLimiterTests_Cap.Value);
}

// Generates an integer seed used to derive a deterministic integrator ID.
// Avoids Guid.NewGuid() (non-deterministic, triggers CON107) while giving each
// property invocation an independent integrator ID.
internal sealed class IntegratorSeedStrategy : IStrategyProvider<int>
{
    public Strategy<int> Create()
        => Strategy.Integers<int>(1, 1_000_000);
}

// Generates a pair of distinct integrator IDs with consume counts within their caps.
internal sealed class IndependentConsumeStrategy : IStrategyProvider<IndependentConsumePair>
{
    private const int MaxCount = IngestRateLimiterTests_Cap.Value - 1;

    private static readonly Strategy<IndependentConsumePair> Inner =
        Strategy.Tuples(
                Strategy.Integers<int>(0, MaxCount),
                Strategy.Integers<int>(0, MaxCount),
                Strategy.Integers<int>(1, 1_000_000),
                Strategy.Integers<int>(1_000_001, 2_000_000))
            .Select(t => new IndependentConsumePair(
                IntegratorFromSeed(t.Item3), t.Item1,
                IntegratorFromSeed(t.Item4), t.Item2));

    public Strategy<IndependentConsumePair> Create() => Inner;

    private static Guid IntegratorFromSeed(int seed)
        => new(seed, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}

internal static class IngestRateLimiterTests_Cap
{
    internal const int Value = 10;
}

public sealed record IndependentConsumePair(
    Guid IntegratorA, int CountA,
    Guid IntegratorB, int CountB);
