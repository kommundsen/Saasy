using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Saasy.Metering.Infrastructure.Ingestion;

namespace Saasy.Metering.Infrastructure.Tests;

public sealed class EnvelopeValidatorTests
{
    private static readonly ILogger NullLogger = NullLogger<EnvelopeValidatorTests>.Instance;

    // Anchor: a fully valid envelope passes validation.
    [Fact]
    public void TryValidate_WithValidEnvelope_ReturnsTrue()
    {
        var envelope = ValidEnvelope();

        var result = EnvelopeValidator.TryValidate(envelope, NullLogger, out var reason);

        Assert.True(result);
        Assert.Null(reason);
    }

    // Anchor: empty Guid is a distinct boundary case (not blank-string).
    [Fact]
    public void TryValidate_WithEmptyIntegratorId_ReturnsFalse()
    {
        var envelope = ValidEnvelope() with { IntegratorId = Guid.Empty };

        var result = EnvelopeValidator.TryValidate(envelope, NullLogger, out var reason);

        Assert.False(result);
        Assert.NotNull(reason);
    }

    // Anchor: default DateTimeOffset is a distinct boundary case (not blank-string).
    [Fact]
    public void TryValidate_WithDefaultOccurredAt_ReturnsFalse()
    {
        var envelope = ValidEnvelope() with { OccurredAt = default };

        var result = EnvelopeValidator.TryValidate(envelope, NullLogger, out var reason);

        Assert.False(result);
        Assert.NotNull(reason);
    }

    // Property: for any blank string s and any string field f in
    // {CustomerExternalRef, EventType, DimensionCode, IdempotencyKey},
    // replacing field f with s causes TryValidate to return false.
    // Both parameters are combined into a single strategy to avoid IR exhaustion
    // during shrinking with two independent SampledFrom strategies.
    [Property]
    public void TryValidate_WithAnyBlankStringField_ReturnsFalse(
        [From<BlankFieldStrategy>] BlankFieldCase testCase)
    {
        var envelope = testCase.FieldSelector switch
        {
            0 => ValidEnvelope() with { CustomerExternalRef = testCase.BlankValue },
            1 => ValidEnvelope() with { EventType = testCase.BlankValue },
            2 => ValidEnvelope() with { DimensionCode = testCase.BlankValue },
            _ => ValidEnvelope() with { IdempotencyKey = testCase.BlankValue },
        };

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

// A blank string paired with the field selector index (0-3) to apply it to.
public sealed record BlankFieldCase(string? BlankValue, int FieldSelector);

// Generates one of all (blank string, field selector) cross-product combinations
// as a single SampledFrom draw. This avoids the "Replay IR exhausted" issue that
// occurs when two independent SampledFrom strategies are composed via SelectMany
// during Conjecture's shrinking phase.
internal sealed class BlankFieldStrategy : IStrategyProvider<BlankFieldCase>
{
    private static readonly IReadOnlyList<string?> BlankStrings =
        new string?[] { null, "", " ", "\t", "\n", "   ", " \t " };

    private static readonly IReadOnlyList<int> FieldSelectors = [0, 1, 2, 3];

    private static readonly IReadOnlyList<BlankFieldCase> AllCombinations =
        BlankStrings.SelectMany(s => FieldSelectors.Select(f => new BlankFieldCase(s, f)))
            .ToList();

    public Strategy<BlankFieldCase> Create()
        => Strategy.SampledFrom(AllCombinations);
}
