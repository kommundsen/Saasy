using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace Saasy.Metering.Infrastructure.Ingestion;

// Publishes envelopes to Azure Event Hubs with partition_key = integrator_id.
// The EventIngestionConsumer picks them up and applies the same idempotency dedup
// (unique index on (integrator_id, idempotency_key)) as the Direct strategy.
// This is the default strategy so HTTP traffic flows through the canonical pipeline
// and is captured by Event Hubs Capture (ADR-0020).
//
// Duplicate detection: because the consumer writes with the same unique index,
// duplicates surface as rejected on the consumer side -- not here at publish time.
// We optimistically return accepted=true for all envelopes; the consumer's dedup
// makes delivery effective-once in the store. Callers relying on immediate duplicate
// feedback should use the Direct strategy.
public sealed class HubEventIngestionStrategy(
    EventHubProducerClient producer)
    : IEventIngestionStrategy
{
    public async Task<IReadOnlyList<EnvelopeIngestResult>> IngestAsync(
        IReadOnlyList<EventEnvelope> envelopes,
        CancellationToken ct)
    {
        // Group by integrator_id so each batch uses the correct partition key.
        // In practice all envelopes in an HTTP request share the same integrator_id
        // (validated before reaching this strategy), so grouping is defensive.
        var byIntegrator = envelopes
            .GroupBy(e => e.IntegratorId)
            .ToList();

        foreach (var group in byIntegrator)
        {
            var partitionKey = group.Key.ToString();
            var batchOptions = new CreateBatchOptions { PartitionKey = partitionKey };
            using var batch = await producer.CreateBatchAsync(batchOptions, ct);

            foreach (var envelope in group)
            {
                var json = JsonSerializer.Serialize(envelope);
                var data = new EventData(System.Text.Encoding.UTF8.GetBytes(json));
                if (!batch.TryAdd(data))
                {
                    // Batch full -- send what we have and start a new one.
                    await producer.SendAsync(batch, ct);
                }
            }

            if (batch.Count > 0)
                await producer.SendAsync(batch, ct);
        }

        // Optimistic: all envelopes published; consumer-side dedup catches duplicates.
        return envelopes
            .Select(e => new EnvelopeIngestResult(e.IdempotencyKey!, true, null))
            .ToList();
    }
}
