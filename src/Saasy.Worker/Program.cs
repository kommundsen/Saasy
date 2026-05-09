using Saasy.Tenancy.Infrastructure.Extensions;
using Saasy.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddTenancyInfrastructure();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

await host.Services.MigrateTenancyAsync();

host.Run();
