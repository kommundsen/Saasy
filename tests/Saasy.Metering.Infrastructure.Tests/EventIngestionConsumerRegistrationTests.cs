using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Saasy.Metering.Infrastructure.Ingestion;

namespace Saasy.Metering.Infrastructure.Tests;

public sealed class EventIngestionConsumerRegistrationTests
{
    [Fact]
    public void AddHostedService_EventIngestionConsumer_RegistersWithoutException()
    {
        // EventIngestionConsumer depends on EventProcessorClient and BlobServiceClient.
        // For the smoke test, register stub clients that are constructed but never connected.
        // The consumer is never started -- only DI wiring is verified.
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddDbContext<MeteringDbContext>(opts =>
            opts.UseInMemoryDatabase("smoke-test"));

        // Use Uri-based overloads so no connection parsing occurs at construction time.
        var sharedKeyCredential = new Azure.Storage.StorageSharedKeyCredential(
            "devstoreaccount1",
            "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tiqnVgrZjgEc/9Xk5SkYSimplyTest==");

        var blobServiceClient = new BlobServiceClient(
            new Uri("http://127.0.0.1:10000/devstoreaccount1"),
            sharedKeyCredential);

        var containerClient = new BlobContainerClient(
            new Uri("http://127.0.0.1:10000/devstoreaccount1/saasy-ingest-checkpoints"),
            sharedKeyCredential);

        services.AddSingleton(blobServiceClient);
        services.AddSingleton(_ => new EventProcessorClient(
            containerClient,
            consumerGroup: "saasy-ingest",
            connectionString: "Endpoint=sb://fake.servicebus.windows.net/;" +
                              "SharedAccessKeyName=RootManageSharedAccessKey;" +
                              "SharedAccessKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=",
            eventHubName: "events"));

        services.AddHostedService<EventIngestionConsumer>();

        // Act -- build the service provider; this confirms all dependencies resolve.
        using var provider = services.BuildServiceProvider(validateScopes: true);

        // Resolving IHostedService confirms the consumer is properly wired.
        var hostedServices = provider.GetServices<IHostedService>();
        Assert.Contains(hostedServices, s => s is EventIngestionConsumer);
    }
}
