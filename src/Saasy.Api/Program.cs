using Saasy.Tenancy.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddTenancyInfrastructure();

var app = builder.Build();

await app.Services.MigrateTenancyAsync();

app.MapGet("/health", () => Results.Ok());

app.Run();
