using System.Text.Json;
using System.Text.Json.Serialization;

namespace Saasy.Metering.Infrastructure.Ingestion;

// Wire format for events arriving on the Event Hubs ingestion stream (ADR-0001).
// Fields match the JSON names the Integrator publishes; snake_case by convention.
// Record to support non-destructive mutation in tests (with-expression).
public sealed record EventEnvelope
{
    [JsonPropertyName("integrator_id")]
    public Guid IntegratorId { get; init; }

    [JsonPropertyName("customer_external_ref")]
    public string? CustomerExternalRef { get; init; }

    [JsonPropertyName("event_type")]
    public string? EventType { get; init; }

    [JsonPropertyName("dimension_code")]
    public string? DimensionCode { get; init; }

    // decimal -- matches MeteringEvent.Value; exact arithmetic for usage quantities.
    [JsonPropertyName("value")]
    public decimal Value { get; init; }

    [JsonPropertyName("occurred_at")]
    public DateTimeOffset OccurredAt { get; init; }

    [JsonPropertyName("idempotency_key")]
    public string? IdempotencyKey { get; init; }

    // Raw JSON object stored as JSONB; nullable because the field is optional.
    [JsonPropertyName("payload")]
    public JsonElement? Payload { get; init; }
}
