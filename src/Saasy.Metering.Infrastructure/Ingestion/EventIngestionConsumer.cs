using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Saasy.Metering.Infrastructure.Events;

namespace Saasy.Metering.Infrastructure.Ingestion;

public sealed class EventIngestionConsumer : BackgroundService
{
    internal static readonly ActivitySource ActivitySource = new("Saasy.Metering.Ingestion");
    private static readonly Meter Meter = new Meter("Saasy.Metering.Ingestion");
    private static readonly Counter<long> PoisonEventCounter =
        Meter.CreateCounter<long>("metering.poison_events", description: "Events rejected due to invalid envelope shape.");
    private static readonly Counter<long> DuplicateEventCounter =
        Meter.CreateCounter<long>("metering.duplicate_events", description: "Events dropped due to idempotency key collision.");

    private readonly EventProcessorClient _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventIngestionConsumer> _logger;
    private readonly Azure.Storage.Blobs.BlobContainerClient _checkpointContainer;

    public EventIngestionConsumer(
        EventProcessorClient processor,
        Azure.Storage.Blobs.BlobServiceClient blobServiceClient,
        IServiceScopeFactory scopeFactory,
        ILogger<EventIngestionConsumer> logger)
    {
        _processor = processor;
        _checkpointContainer = blobServiceClient.GetBlobContainerClient(
            Extensions.ServiceCollectionExtensions.CheckpointContainerName);
        _scopeFactory = scopeFactory;
        _logger = logger;

        _processor.ProcessEventAsync += ProcessEventAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // EventProcessorClient requires the container to exist before starting.
        await _checkpointContainer.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

        await _processor.StartProcessingAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { }
        finally
        {
            await _processor.StopProcessingAsync();
        }
    }

    private async Task ProcessEventAsync(ProcessEventArgs args)
    {
        if (!args.HasEvent)
            return;

        using var activity = ActivitySource.StartActivity("ProcessEvent");

        EventEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<EventEnvelope>(
                args.Data.EventBody.ToMemory().Span,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            PoisonEventCounter.Add(1);
            _logger.LogWarning(ex, "Poison event: JSON deserialization failed on partition {Partition}", args.Partition.PartitionId);
            await args.UpdateCheckpointAsync(args.CancellationToken);
            return;
        }

        if (envelope is null)
        {
            PoisonEventCounter.Add(1);
            _logger.LogWarning("Poison event: deserialized to null on partition {Partition}", args.Partition.PartitionId);
            await args.UpdateCheckpointAsync(args.CancellationToken);
            return;
        }

        if (!EnvelopeValidator.TryValidate(envelope, _logger, out _))
        {
            PoisonEventCounter.Add(1);
            await args.UpdateCheckpointAsync(args.CancellationToken);
            return;
        }

        JsonDocument? payloadDocument = null;
        if (envelope.Payload.HasValue && envelope.Payload.Value.ValueKind != System.Text.Json.JsonValueKind.Null)
        {
            payloadDocument = JsonDocument.Parse(envelope.Payload.Value.GetRawText());
        }

        var meteringEvent = MeteringEvent.Create(
            envelope.IntegratorId,
            envelope.CustomerExternalRef!,
            envelope.EventType!,
            envelope.DimensionCode!,
            envelope.Value,
            envelope.OccurredAt,
            envelope.IdempotencyKey!,
            payloadDocument);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MeteringDbContext>();

        try
        {
            db.Events.Add(meteringEvent);
            await db.SaveChangesAsync(args.CancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            DuplicateEventCounter.Add(1);
            // Truncate integrator_id to first 8 chars of string representation to avoid
            // logging the full value in contexts where log aggregation is shared.
            var truncatedIntegratorId = envelope.IntegratorId.ToString()[..8];
            _logger.LogDebug(
                "Duplicate event dropped: integrator={TruncatedIntegratorId}... key={IdempotencyKey}",
                truncatedIntegratorId,
                envelope.IdempotencyKey);
        }

        await args.UpdateCheckpointAsync(args.CancellationToken);
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "EventProcessorClient error on partition {Partition} operation {Operation}",
            args.PartitionId,
            args.Operation);
        return Task.CompletedTask;
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException { SqlState: "23505" };
    }
}
