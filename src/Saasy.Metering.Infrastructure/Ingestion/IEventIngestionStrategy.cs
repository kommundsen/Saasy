namespace Saasy.Metering.Infrastructure.Ingestion;

// Port for writing a validated batch of envelopes.
// Two implementations: Direct (DB write) and Hub (Event Hubs publish).
// Wire the desired implementation via appsettings "Ingestion:Strategy": "Direct" | "Hub".
// Default is "Hub" so HTTP traffic flows through the canonical pipeline (ADR-0001, ADR-0020).
public interface IEventIngestionStrategy
{
    // Attempts to persist each envelope.
    // Returns per-envelope results: accepted=true when new, accepted=false when duplicate.
    Task<IReadOnlyList<EnvelopeIngestResult>> IngestAsync(
        IReadOnlyList<EventEnvelope> envelopes,
        CancellationToken ct);
}

public sealed record EnvelopeIngestResult(
    string IdempotencyKey,
    bool Accepted,
    string? Reason);
