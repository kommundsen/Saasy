var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .AddDatabase("saasy");

builder.AddProject<Projects.Saasy_Api>("api")
    .WithHttpHealthCheck("/health")
    .WithReference(postgres)
    .WaitFor(postgres);

builder.AddProject<Projects.Saasy_Worker>("worker")
    .WithReference(postgres)
    .WaitFor(postgres);

builder.Build().Run();
