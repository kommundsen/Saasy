using Saasy.Api.Customers;
using Saasy.Api.Integrators;
using Saasy.Metering.Infrastructure.Extensions;
using Saasy.Tenancy.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddTenancyInfrastructure();

// Register MeteringDbContext so Api can apply migrations at startup.
// The Api does NOT host EventIngestionConsumer -- that lives in Worker.
builder.AddNpgsqlMeteringDbContext();

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

app.Run();

// Expose Program for WebApplicationFactory in integration tests.
public partial class Program { }
