using Saasy.Api.Customers;
using Saasy.Api.Integrators;
using Saasy.Tenancy.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddTenancyInfrastructure();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
    await app.Services.MigrateTenancyAsync();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapIntegratorEndpoints();
app.MapCustomerEndpoints();

app.Run();

// Expose Program for WebApplicationFactory in integration tests.
public partial class Program { }
