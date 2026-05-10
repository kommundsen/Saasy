using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Saasy.Metering.Infrastructure.Ingestion;

namespace Saasy.Metering.Infrastructure.Tests;

public sealed class EnvelopeValidatorTests
{
    private static readonly ILogger NullLogger = NullLogger<EnvelopeValidatorTests>.Instance;

    [Fact]
    public void TryValidate_WithValidEnvelope_ReturnsTrue()
    {
        var envelope = ValidEnvelope();

        var result = EnvelopeValidator.TryValidate(envelope, NullLogger, out var reason);

        Assert.True(result);
        Assert.Null(reason);
    }

    [Fact]
    public void TryValidate_WithEmptyIntegratorId_ReturnsFalse()
    {
        var envelope = ValidEnvelope() with { IntegratorId = Guid.Empty };

        var result = EnvelopeValidator.TryValidate(envelope, NullLogger, out var reason);

        Assert.False(result);
        Assert.NotNull(reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryValidate_WithMissingCustomerExternalRef_ReturnsFalse(string? value)
    {
        var envelope = ValidEnvelope() with { CustomerExternalRef = value };

        var result = EnvelopeValidator.TryValidate(envelope, NullLogger, out var reason);

        Assert.False(result);
        Assert.NotNull(reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryValidate_WithMissingEventType_ReturnsFalse(string? value)
    {
        var envelope = ValidEnvelope() with { EventType = value };

        var result = EnvelopeValidator.TryValidate(envelope, NullLogger, out var reason);

        Assert.False(result);
        Assert.NotNull(reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryValidate_WithMissingDimensionCode_ReturnsFalse(string? value)
    {
        var envelope = ValidEnvelope() with { DimensionCode = value };

        var result = EnvelopeValidator.TryValidate(envelope, NullLogger, out var reason);

        Assert.False(result);
        Assert.NotNull(reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryValidate_WithMissingIdempotencyKey_ReturnsFalse(string? value)
    {
        var envelope = ValidEnvelope() with { IdempotencyKey = value };

        var result = EnvelopeValidator.TryValidate(envelope, NullLogger, out var reason);

        Assert.False(result);
        Assert.NotNull(reason);
    }

    [Fact]
    public void TryValidate_WithDefaultOccurredAt_ReturnsFalse()
    {
        var envelope = ValidEnvelope() with { OccurredAt = default };

        var result = EnvelopeValidator.TryValidate(envelope, NullLogger, out var reason);

        Assert.False(result);
        Assert.NotNull(reason);
    }

    private static EventEnvelope ValidEnvelope() => new()
    {
        IntegratorId = Guid.NewGuid(),
        CustomerExternalRef = "cust-001",
        EventType = "api_call",
        DimensionCode = "api_calls",
        Value = 1.0m,
        OccurredAt = DateTimeOffset.UtcNow,
        IdempotencyKey = "key-abc",
        Payload = null,
    };
}
