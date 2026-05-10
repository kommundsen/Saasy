using System.Text.Json;

namespace Saasy.Metering.Infrastructure.Events;

// Value is stored as decimal (not double) to preserve exact representation for
// usage quantities that feed into monetary calculations. Floating-point rounding
// errors accumulate across aggregation and produce incorrect Invoice amounts.
public sealed class MeteringEvent
{
    public Guid Id { get; private set; }
    public Guid IntegratorId { get; private set; }
    public string CustomerExternalRef { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string DimensionCode { get; private set; } = string.Empty;
    public decimal Value { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public JsonDocument? Payload { get; private set; }
    public DateTimeOffset IngestedAt { get; private set; }

    private MeteringEvent() { }

    public static MeteringEvent Create(
        Guid integratorId,
        string customerExternalRef,
        string eventType,
        string dimensionCode,
        decimal value,
        DateTimeOffset occurredAt,
        string idempotencyKey,
        JsonDocument? payload)
    {
        return new MeteringEvent
        {
            Id = Guid.NewGuid(),
            IntegratorId = integratorId,
            CustomerExternalRef = customerExternalRef,
            EventType = eventType,
            DimensionCode = dimensionCode,
            Value = value,
            OccurredAt = occurredAt,
            IdempotencyKey = idempotencyKey,
            Payload = payload,
            IngestedAt = DateTimeOffset.UtcNow,
        };
    }
}
