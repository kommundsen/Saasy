using Saasy.Api.Customers;
using Saasy.Api.Events;
using Saasy.Api.Integrators;
using Saasy.Metering.Infrastructure.Extensions;
using Saasy.Tenancy.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddTenancyInfrastructure();

// Register MeteringDbContext so Api can apply migrations at startup.
// The Api does NOT host EventIngestionConsumer -- that lives in Worker.
builder.AddNpgsqlMeteringDbContext();

// HTTP ingestion path: rate limiter + ingestion strategy (Hub or Direct per config).
builder.AddHttpEventIngestion();

// ASP.NET Core built-in OpenAPI generation (.NET 9+ -- no Swashbuckle).
builder.Services.AddOpenApi();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    await app.Services.MigrateTenancyAsync();
    await app.Services.MigrateMeteringAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapIntegratorEndpoints();
app.MapCustomerEndpoints();
app.MapEventIngestionEndpoints();

// Serve OpenAPI spec at /openapi/v1.json (built-in .NET 9+ endpoint).
app.MapOpenApi();

app.Run();

// Expose Program for WebApplicationFactory in integration tests.
public partial class Program { }
