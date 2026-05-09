using Microsoft.EntityFrameworkCore;
using Saasy.Tenancy.Infrastructure.Extensions;
using Saasy.Tenancy.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddTenancyInfrastructure();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/health", () => Results.Ok());

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
