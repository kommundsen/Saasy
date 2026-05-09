using Saasy.Tenancy.Infrastructure.Extensions;
using Saasy.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddTenancyInfrastructure();

builder.Services.AddHostedService<HeartbeatWorker>();

var host = builder.Build();

await host.Services.MigrateTenancyAsync();

host.Run();
