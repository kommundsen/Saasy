using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Saasy.Metering.Infrastructure.Events;

namespace Saasy.Metering.Infrastructure.Ingestion;

// Writes envelopes directly to metering.events via EF Core.
// Each envelope is inserted individually so we can distinguish duplicate-key
// outcomes per envelope without rolling back the whole batch.
// This is the opt-in strategy for tests and very-low-volume Integrators.
public sealed class DirectEventIngestionStrategy(
    MeteringDbContext db,
    ILogger<DirectEventIngestionStrategy> logger)
    : IEventIngestionStrategy
{
    public async Task<IReadOnlyList<EnvelopeIngestResult>> IngestAsync(
        IReadOnlyList<EventEnvelope> envelopes,
        CancellationToken ct)
    {
        var results = new List<EnvelopeIngestResult>(envelopes.Count);

        foreach (var envelope in envelopes)
        {
            var key = envelope.IdempotencyKey!;
            var integratorId = envelope.IntegratorId;

            // Check for an existing event with the same (integrator_id, idempotency_key).
            // This explicit check mirrors the unique index constraint in Postgres and also
            // handles InMemory providers (used in tests) that do not enforce unique indexes.
            var alreadyExists = await db.Events.AnyAsync(
                e => e.IntegratorId == integratorId && e.IdempotencyKey == key,
                ct);

            if (alreadyExists)
            {
                logger.LogDebug(
                    "Duplicate event skipped: integrator={IntegratorId} key={Key}",
                    integratorId.ToString()[..8],
                    key);
                results.Add(new EnvelopeIngestResult(key, false, "duplicate"));
                continue;
            }

            JsonDocument? payloadDoc = null;
            if (envelope.Payload.HasValue
                && envelope.Payload.Value.ValueKind != JsonValueKind.Null)
            {
                payloadDoc = JsonDocument.Parse(envelope.Payload.Value.GetRawText());
            }

            var meteringEvent = MeteringEvent.Create(
                integratorId,
                envelope.CustomerExternalRef!,
                envelope.EventType!,
                envelope.DimensionCode!,
                envelope.Value,
                envelope.OccurredAt,
                key,
                payloadDoc);

            try
            {
                db.Events.Add(meteringEvent);
                await db.SaveChangesAsync(ct);
                results.Add(new EnvelopeIngestResult(key, true, null));
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Race condition: another request inserted between our check and insert.
                // Detach the conflicting entry so subsequent saves are not poisoned.
                db.ChangeTracker.Clear();
                logger.LogDebug(
                    "Duplicate event skipped (race): integrator={IntegratorId} key={Key}",
                    integratorId.ToString()[..8],
                    key);
                results.Add(new EnvelopeIngestResult(key, false, "duplicate"));
            }
        }

        return results;
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: "23505" };
}
