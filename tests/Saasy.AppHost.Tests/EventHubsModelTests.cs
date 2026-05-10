using Aspire.Hosting;
using Aspire.Hosting.Azure;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Saasy.AppHost.Tests;

// Model-level smoke tests for the AppHost Event Hubs wiring.
// These tests operate on the application model only -- no Docker, no emulator process.
public sealed class EventHubsModelTests
{
    [Fact]
    public void AddAzureEventHubs_WithRunAsEmulator_RegistersNamespaceResource()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddAzureEventHubs("eventhubns")
            .RunAsEmulator();

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var resource = model.Resources
            .OfType<AzureEventHubsResource>()
            .FirstOrDefault(r => r.Name == "eventhubns");

        Assert.NotNull(resource);
        Assert.True(resource.IsEmulator);
    }

    [Fact]
    public void AddHub_RegistersHubChildResource()
    {
        var builder = DistributedApplication.CreateBuilder();

        var ns = builder.AddAzureEventHubs("eventhubns")
            .RunAsEmulator();

        ns.AddHub("events");

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var hub = model.Resources
            .OfType<AzureEventHubResource>()
            .FirstOrDefault(r => r.Name == "events");

        Assert.NotNull(hub);
    }

    [Fact]
    public void AddConsumerGroup_RegistersConsumerGroupChildResource()
    {
        var builder = DistributedApplication.CreateBuilder();

        var ns = builder.AddAzureEventHubs("eventhubns")
            .RunAsEmulator();

        var hub = ns.AddHub("events");
        hub.AddConsumerGroup("saasy-ingest-cg", "saasy-ingest");

        using var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var cg = model.Resources
            .OfType<AzureEventHubConsumerGroupResource>()
            .FirstOrDefault(r => r.Name == "saasy-ingest-cg");

        Assert.NotNull(cg);
    }
}
