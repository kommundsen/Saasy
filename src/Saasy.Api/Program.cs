using Saasy.Tenancy.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddTenancyInfrastructure();

var app = builder.Build();

await app.Services.MigrateTenancyAsync();

app.MapDefaultEndpoints();

app.Run();
