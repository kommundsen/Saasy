var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .AddDatabase("saasy");

// Per-environment Capture container name (ADR-0020):
//   sandbox -> event-capture-sandbox
//   prod    -> event-capture-prod
// The value flows into the synthesised Bicep as a parameter so each azd
// environment supplies its own value via azure.yaml / parameter files.
// Local dev uses the emulator, which ignores the ConfigureInfrastructure
// block entirely, so the default value here is never actually used at run
// time -- it exists only to satisfy the ParameterResource API.
var captureContainerParam = builder.AddParameter("eventHubsCaptureContainer", secret: false);

// One Event Hubs namespace per region (ADR-0001, ADR-0009).
// In local dev: Event Hubs emulator via RunAsEmulator().
// In publish mode (azd): synthesised Bicep provisions the real namespace.
// Capture (ADR-0020) is configured via ConfigureInfrastructure below;
// the emulator does not support Capture, so that hook is publish-only.
var eventHubsNamespace = builder.AddAzureEventHubs("eventhubns")
    .RunAsEmulator()
    .ConfigureInfrastructure(infra =>
    {
        // Enable Event Hubs Capture on the ingest hub per ADR-0020.
        // Capture writes to a Storage Account container provisioned alongside
        // this namespace. The container name is supplied as a Bicep parameter
        // so sandbox and production each resolve to their own container
        // (event-capture-sandbox / event-capture-prod) without hard-coding.
        var captureContainerBicepParam =
            captureContainerParam.AsProvisioningParameter(infra, "eventHubsCaptureContainer");

        var hubs = infra.GetProvisionableResources()
            .OfType<Azure.Provisioning.EventHubs.EventHub>()
            .ToList();

        foreach (var hub in hubs)
        {
            hub.CaptureDescription = new Azure.Provisioning.EventHubs.CaptureDescription
            {
                Enabled = true,
                Encoding = Azure.Provisioning.EventHubs.EncodingCaptureDescription.Avro,
                IntervalInSeconds = 300,
                SizeLimitInBytes = 314572800, // 300 MB
                SkipEmptyArchives = true,
                Destination = new Azure.Provisioning.EventHubs.EventHubDestination
                {
                    Name = "EventHubArchive.AzureBlockBlob",
                    BlobContainer = captureContainerBicepParam,
                    ArchiveNameFormat =
                        "{Namespace}/{EventHub}/{PartitionId}/{Year}/{Month}/{Day}/{Hour}/{Minute}/{Second}"
                }
            };
        }
    });

var eventsHub = eventHubsNamespace.AddHub("events");
eventsHub.AddConsumerGroup("saasy-ingest-cg", "saasy-ingest");

// Api owns migrations in dev; production uses a dedicated Saasy.Migrate host
// (per docs/architecture/architecture.md §Migrations).
var api = builder.AddProject<Projects.Saasy_Api>("api")
    .WithHttpHealthCheck("/health")
    .WithReference(postgres)
    .WaitFor(postgres);

// Worker is the Event Hubs consumer per ADR-0001.
builder.AddProject<Projects.Saasy_Worker>("worker")
    .WithReference(postgres)
    .WithReference(eventHubsNamespace)
    .WaitFor(postgres)
    .WaitFor(api);

builder.Build().Run();
