using Saasy.Metering.Infrastructure.Ingestion;

namespace Saasy.Metering.Infrastructure.Tests;

public sealed class IngestRateLimiterTests
{
    [Fact]
    public void TryConsume_WithinCap_ReturnsTrue()
    {
        var limiter = new IngestRateLimiter();
        var integrator = Guid.NewGuid();

        Assert.True(limiter.TryConsume(integrator, capPerMinute: 5));
    }

    [Fact]
    public void TryConsume_ExceedsCap_ReturnsFalse()
    {
        var limiter = new IngestRateLimiter();
        var integrator = Guid.NewGuid();

        // Consume all 3 allowed requests
        for (var i = 0; i < 3; i++)
            limiter.TryConsume(integrator, capPerMinute: 3);

        // 4th must be rejected
        Assert.False(limiter.TryConsume(integrator, capPerMinute: 3));
    }

    [Fact]
    public void TryConsume_DifferentIntegrators_CountsAreIndependent()
    {
        var limiter = new IngestRateLimiter();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        // Exhaust integrator A's cap
        for (var i = 0; i < 2; i++)
            limiter.TryConsume(a, capPerMinute: 2);

        Assert.False(limiter.TryConsume(a, capPerMinute: 2));

        // Integrator B is unaffected
        Assert.True(limiter.TryConsume(b, capPerMinute: 2));
    }

    [Fact]
    public void SecondsUntilWindowReset_NoEntry_Returns60()
    {
        var limiter = new IngestRateLimiter();
        var integrator = Guid.NewGuid();

        Assert.Equal(60, limiter.SecondsUntilWindowReset(integrator));
    }

    [Fact]
    public void SecondsUntilWindowReset_AfterConsume_IsPositiveAndAtMost60()
    {
        var limiter = new IngestRateLimiter();
        var integrator = Guid.NewGuid();

        limiter.TryConsume(integrator, capPerMinute: 10);

        var remaining = limiter.SecondsUntilWindowReset(integrator);
        Assert.InRange(remaining, 1, 60);
    }
}
