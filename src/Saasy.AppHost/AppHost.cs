var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Saasy_Api>("api");
builder.AddProject<Projects.Saasy_Worker>("worker");

builder.Build().Run();
