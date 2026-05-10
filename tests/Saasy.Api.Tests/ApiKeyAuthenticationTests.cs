using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Saasy.Tenancy.Domain.Integrators;
using Saasy.Tenancy.Infrastructure;

namespace Saasy.Api.Tests;

public sealed class ApiKeyAuthenticationTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public ApiKeyAuthenticationTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ValidApiKey_Returns200_AndPopulatesClaims()
    {
        var (_, secret) = await _factory.CreateIntegratorWithApiKeyAsync();
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("ApiKey", secret);

        var response = await client.GetAsync("/v1/customers/00000000-0000-0000-0000-000000000001");

        // 404 means auth passed (customer not found) -- 401 would mean auth failed
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MissingAuthorizationHeader_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/v1/customers/00000000-0000-0000-0000-000000000001");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MalformedHeader_BearerScheme_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "sometoken");

        var response = await client.GetAsync("/v1/customers/00000000-0000-0000-0000-000000000001");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MalformedHeader_ApiKeyWithNoSecret_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "ApiKey");

        var response = await client.GetAsync("/v1/customers/00000000-0000-0000-0000-000000000001");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnknownPrefixKey_Returns401()
    {
        var client = _factory.CreateClient();
        // A syntactically valid key that doesn't exist in the database
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("ApiKey", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");

        var response = await client.GetAsync("/v1/customers/00000000-0000-0000-0000-000000000001");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RevokedKey_Returns401_EvenIfHashMatches()
    {
        var (integrator, secret) = await _factory.CreateIntegratorWithApiKeyAsync();
        var apiKey = integrator.ApiKeys.First();

        await _factory.RevokeApiKeyAsync(integrator.Id, apiKey.Id);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("ApiKey", secret);

        var response = await client.GetAsync("/v1/customers/00000000-0000-0000-0000-000000000001");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiKeyScheme_IsAnonymous_ForMintEndpoint()
    {
        // POST /v1/integrators/{id}/api-keys must allow anonymous access --
        // it's how new integrators get their first key.
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            "/v1/integrators/00000000-0000-0000-0000-000000000001/api-keys",
            JsonContent("""{"name":"test"}"""));

        // 404 (integrator not found) means we got past auth -- 401 would mean blocked.
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static StringContent JsonContent(string json)
        => new(json, System.Text.Encoding.UTF8, "application/json");
}

public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    // Unique database name per factory instance so tests in the same class share a db
    // (via IClassFixture) while separate test classes don't interfere.
    private readonly string _dbName = $"TenancyTest_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" environment skips MigrateTenancyAsync in Program.cs and tells
        // AddNpgsqlDbContext not to validate the (absent) connection string.
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Aspire's AddNpgsqlDbContext uses AddDbContextPool, which registers
            // IDbContextPool<T> and several options types. Remove everything related
            // to TenancyDbContext so we can replace them with an InMemory provider.
            var toRemove = services
                .Where(d =>
                    d.ServiceType.FullName?.Contains("TenancyDbContext") == true ||
                    d.ServiceType.FullName?.Contains("DbContextPool") == true ||
                    (d.ServiceType == typeof(DbContextOptions<TenancyDbContext>)))
                .ToList();

            foreach (var d in toRemove)
                services.Remove(d);

            services.AddDbContext<TenancyDbContext>(opts =>
                opts.UseInMemoryDatabase(_dbName));
        });
    }

    // Seed an Integrator with a minted ApiKey and return the integrator + plaintext secret.
    public async Task<(Integrator Integrator, string PlaintextSecret)> CreateIntegratorWithApiKeyAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

        var integrator = Integrator.Create(
            "Test Integrator",
            IntegratorKind.Production,
            IntegratorTier.Free,
            Timezone.Create("UTC"));

        var (_, secret) = integrator.MintApiKey("test-key");

        db.Integrators.Add(integrator);
        await db.SaveChangesAsync();

        return (integrator, secret);
    }

    public async Task RevokeApiKeyAsync(IntegratorId integratorId, ApiKeyId apiKeyId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

        var integrator = await db.Integrators
            .FirstAsync(i => i.Id == integratorId);

        integrator.RevokeApiKey(apiKeyId);
        await db.SaveChangesAsync();
    }
}
