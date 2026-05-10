using Saasy.Metering.Infrastructure.Extensions;
using Saasy.Tenancy.Infrastructure.Extensions;
using Saasy.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddTenancyInfrastructure();
builder.AddMeteringInfrastructure();

builder.Services.AddHostedService<HeartbeatWorker>();

// Migrations are owned by Saasy.Api in dev (and by Saasy.Migrate in production
// per docs/architecture/architecture.md §Migrations). Worker WaitFor(api) in
// AppHost.cs guarantees the schema is in place before this host starts.
var host = builder.Build();

host.Run();
