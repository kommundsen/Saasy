using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Saasy.Metering.Infrastructure.Ingestion;

namespace Saasy.Metering.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    // Container name used for EventProcessorClient checkpoint/ownership storage.
    // One container per Event Hub + consumer group combination (per Azure docs).
    internal const string CheckpointContainerName = "saasy-ingest-checkpoints";

    // Registers only the MeteringDbContext -- for hosts that apply migrations
    // but do not host the ingestion consumer (e.g. Saasy.Api).
    public static IHostApplicationBuilder AddNpgsqlMeteringDbContext(
        this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<MeteringDbContext>(
            connectionName: "saasy",
            configureDbContextOptions: opts =>
                opts.UseNpgsql(npgsql =>
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema: "metering")));

        return builder;
    }

    // Registers MeteringDbContext, BlobServiceClient, EventProcessorClient, and
    // the EventIngestionConsumer BackgroundService -- for Saasy.Worker.
    public static IHostApplicationBuilder AddMeteringInfrastructure(
        this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlMeteringDbContext();

        builder.AddAzureBlobServiceClient(connectionName: "checkpointstore");

        builder.Services.AddSingleton<EventProcessorClient>(sp =>
        {
            var blobServiceClient = sp.GetRequiredService<BlobServiceClient>();
            var containerClient = blobServiceClient.GetBlobContainerClient(CheckpointContainerName);

            var connectionString = builder.Configuration["ConnectionStrings:eventhubns"]
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:eventhubns is required for EventProcessorClient. " +
                    "Ensure the Worker project references the eventhubns resource via AppHost.");

            // Use the connection string directly; EventProcessorClient parses the
            // fully-qualified namespace from the Endpoint= component internally.
            return new EventProcessorClient(
                containerClient,
                consumerGroup: "saasy-ingest",
                connectionString: connectionString,
                eventHubName: "events");
        });

        builder.Services.AddHostedService<EventIngestionConsumer>();

        return builder;
    }

    public static async Task MigrateMeteringAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MeteringDbContext>();

        // Ensure schema exists before EF reads the migration history table.
        await db.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS metering;");

        await db.Database.MigrateAsync();
    }
}
