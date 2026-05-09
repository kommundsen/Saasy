var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .AddDatabase("saasy");

// Api owns migrations in dev; production uses a dedicated Saasy.Migrate host
// (per docs/architecture/architecture.md §Migrations).
var api = builder.AddProject<Projects.Saasy_Api>("api")
    .WithHttpHealthCheck("/health")
    .WithReference(postgres)
    .WaitFor(postgres);

builder.AddProject<Projects.Saasy_Worker>("worker")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WaitFor(api);

builder.Build().Run();
