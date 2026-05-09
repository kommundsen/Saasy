using Saasy.Tenancy.Infrastructure.Extensions;
using Saasy.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddTenancyInfrastructure();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
