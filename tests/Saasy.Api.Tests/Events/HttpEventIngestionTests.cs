using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Saasy.Metering.Infrastructure;
using Saasy.Metering.Infrastructure.Ingestion;
using Saasy.Tenancy.Domain.Integrators;
using Saasy.Tenancy.Infrastructure;

namespace Saasy.Api.Tests.Events;

public sealed class HttpEventIngestionTests : IClassFixture<EventIngestionTestFactory>
{
    private readonly EventIngestionTestFactory _factory;

    public HttpEventIngestionTests(EventIngestionTestFactory factory)
    {
        _factory = factory;
    }

    // --- Auth ---

    [Fact]
    public async Task MissingApiKey_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/v1/events",
            Json("""{"integrator_id":"00000000-0000-0000-0000-000000000001","customer_external_ref":"c","event_type":"api_call","dimension_code":"api_calls","value":1,"occurred_at":"2026-01-01T00:00:00Z","idempotency_key":"k1"}"""),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- Single envelope happy path ---

    [Fact]
    public async Task SingleEnvelope_ValidApiKey_Returns202_WithAcceptedArray()
    {
        var (integrator, secret) = await _factory.CreateIntegratorWithApiKeyAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", secret);

        var key = Guid.NewGuid().ToString();
        var response = await client.PostAsync("/v1/events",
            Json($$"""{"integrator_id":"{{integrator.Id.Value}}","customer_external_ref":"cust-1","event_type":"api_call","dimension_code":"api_calls","value":1,"occurred_at":"2026-01-01T00:00:00Z","idempotency_key":"{{key}}"}"""),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var doc = JsonDocument.Parse(body);
        var results = doc.RootElement.GetProperty("results");
        Assert.Equal(JsonValueKind.Array, results.ValueKind);
        Assert.Equal(1, results.GetArrayLength());
        Assert.True(results[0].GetProperty("accepted").GetBoolean());
        Assert.Equal(key, results[0].GetProperty("idempotency_key").GetString());
    }

    // --- Batch of N (property): for any N in [1, 50], submitting N valid envelopes
    // returns 202 and the response accepted array has exactly N entries.
    // Upper bound is 50 (not 500) to keep per-iteration HTTP cost manageable.
    // The Fact below anchors the 500-item boundary case separately.
    [Property]
    public async Task BatchOfN_Returns202_AcceptedArrayLengthN(
        [From<SmallBatchSizeStrategy>] int n)
    {
        var (integrator, secret) = await _factory.CreateIntegratorWithApiKeyAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", secret);

        var envelopes = Enumerable.Range(1, n).Select(i => new
        {
            integrator_id = integrator.Id.Value,
            customer_external_ref = "cust-1",
            event_type = "api_call",
            dimension_code = "api_calls",
            value = (decimal)i,
            occurred_at = "2026-01-01T00:00:00Z",
            idempotency_key = $"prop-batch-{n}-{i}"
        });

        var response = await client.PostAsync("/v1/events",
            Json(JsonSerializer.Serialize(envelopes)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var doc = JsonDocument.Parse(body);
        var results = doc.RootElement.GetProperty("results");
        Assert.Equal(n, results.GetArrayLength());
    }

    // --- Batch of 500 (boundary anchor) ---

    [Fact]
    public async Task BatchOf500_Returns202_AcceptedArrayLength500()
    {
        var (integrator, secret) = await _factory.CreateIntegratorWithApiKeyAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", secret);

        var envelopes = Enumerable.Range(1, 500).Select(i => new
        {
            integrator_id = integrator.Id.Value,
            customer_external_ref = "cust-1",
            event_type = "api_call",
            dimension_code = "api_calls",
            value = (decimal)i,
            occurred_at = "2026-01-01T00:00:00Z",
            idempotency_key = $"batch500-{i}-{Guid.NewGuid()}"
        });

        var response = await client.PostAsync("/v1/events",
            Json(JsonSerializer.Serialize(envelopes)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var doc = JsonDocument.Parse(body);
        var results = doc.RootElement.GetProperty("results");
        Assert.Equal(500, results.GetArrayLength());
    }

    // --- Batch of 501 -> 400 ---

    [Fact]
    public async Task BatchOf501_Returns400_WithBatchSizeError()
    {
        var (integrator, secret) = await _factory.CreateIntegratorWithApiKeyAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", secret);

        var envelopes = Enumerable.Range(1, 501).Select(i => new
        {
            integrator_id = integrator.Id.Value,
            customer_external_ref = "cust-1",
            event_type = "api_call",
            dimension_code = "api_calls",
            value = (decimal)i,
            occurred_at = "2026-01-01T00:00:00Z",
            idempotency_key = $"batch501-{i}"
        });

        var response = await client.PostAsync("/v1/events",
            Json(JsonSerializer.Serialize(envelopes)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var doc = JsonDocument.Parse(body);
        var errors = doc.RootElement.GetProperty("errors");
        Assert.Equal(JsonValueKind.Array, errors.ValueKind);
        Assert.True(errors.GetArrayLength() > 0);
        var first = errors[0];
        Assert.Equal("batchSize", first.GetProperty("field").GetString());
    }

    // --- Mixed shape: one bad envelope in a batch -> 400 ---

    [Fact]
    public async Task BatchWithOneBadEnvelope_Returns400_WithErrorsIndexed()
    {
        var (integrator, secret) = await _factory.CreateIntegratorWithApiKeyAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", secret);

        // Index 0 is valid, index 1 is missing dimension_code
        var json = $$"""
            [
              {"integrator_id":"{{integrator.Id.Value}}","customer_external_ref":"c","event_type":"api_call","dimension_code":"api_calls","value":1,"occurred_at":"2026-01-01T00:00:00Z","idempotency_key":"good-key-1"},
              {"integrator_id":"{{integrator.Id.Value}}","customer_external_ref":"c","event_type":"api_call","value":1,"occurred_at":"2026-01-01T00:00:00Z","idempotency_key":"bad-key-1"}
            ]
            """;

        var response = await client.PostAsync("/v1/events", Json(json),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var doc = JsonDocument.Parse(body);
        var errors = doc.RootElement.GetProperty("errors");
        Assert.Equal(JsonValueKind.Array, errors.ValueKind);
        // Should contain the bad index (1)
        var indices = errors.EnumerateArray()
            .Select(e => e.GetProperty("index").GetInt32())
            .ToList();
        Assert.Contains(1, indices);
        Assert.DoesNotContain(0, indices);
    }

    // --- Duplicate idempotency key property: for any key string k, submitting the
    // same envelope twice yields accepted:false, reason:"duplicate" the second time.
    [Property]
    public async Task DuplicateIdempotencyKey_Returns202_WithAcceptedFalse_ForAnyKey(
        [From<IdempotencyKeySuffixStrategy>] int keySuffix)
    {
        var (integrator, secret) = await _factory.CreateIntegratorWithApiKeyAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("ApiKey", secret);

        var key = $"dup-key-{keySuffix}";
        var payload = $$"""{"integrator_id":"{{integrator.Id.Value}}","customer_external_ref":"cust-1","event_type":"api_call","dimension_code":"api_calls","value":1,"occurred_at":"2026-01-01T00:00:00Z","idempotency_key":"{{key}}"}""";

        var first = await client.PostAsync("/v1/events", Json(payload),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);

        var second = await client.PostAsync("/v1/events", Json(payload),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);

        var body = await second.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var doc = JsonDocument.Parse(body);
        var results = doc.RootElement.GetProperty("results");
        Assert.Equal(1, results.GetArrayLength());
        Assert.False(results[0].GetProperty("accepted").GetBoolean());
        Assert.Equal("duplicate", results[0].GetProperty("reason").GetString());
    }

    // --- Rate cap exceeded -> 429 ---

    [Fact]
    public async Task RateCapExceeded_Returns429_WithRetryAfterHeader()
    {
        var (integrator, secret) = await _factory.CreateIntegratorWithApiKeyAsync(IntegratorTier.Free);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", secret);

        HttpResponseMessage? lastResponse = null;
        // Free tier cap is 10 reqs/min; send 15 requests until we hit 429
        for (var i = 0; i < 15; i++)
        {
            var key = Guid.NewGuid().ToString();
            lastResponse = await client.PostAsync("/v1/events",
                Json($$"""{"integrator_id":"{{integrator.Id.Value}}","customer_external_ref":"cust-1","event_type":"api_call","dimension_code":"api_calls","value":1,"occurred_at":"2026-01-01T00:00:00Z","idempotency_key":"{{key}}"}"""),
                TestContext.Current.CancellationToken);
            if (lastResponse.StatusCode == HttpStatusCode.TooManyRequests)
                break;
        }

        Assert.NotNull(lastResponse);
        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
        Assert.NotNull(lastResponse.Headers.RetryAfter);
    }

    // --- OpenAPI spec ---

    [Fact]
    public async Task OpenApiSpec_ContainsEventsPath()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var doc = JsonDocument.Parse(body);
        var paths = doc.RootElement.GetProperty("paths");
        var hasEventsPath = paths.EnumerateObject()
            .Any(p => p.Name.Contains("/v1/events", StringComparison.OrdinalIgnoreCase));
        Assert.True(hasEventsPath, "OpenAPI spec must contain a path entry for /v1/events");
    }

    private static StringContent Json(string json)
        => new(json, Encoding.UTF8, "application/json");
}

// Generates batch sizes in [1, 50] -- small enough for property-test HTTP overhead.
internal sealed class SmallBatchSizeStrategy : IStrategyProvider<int>
{
    public Strategy<int> Create()
        => Strategy.Integers<int>(1, 50);
}

// Generates integer suffixes used to construct deterministic idempotency keys.
internal sealed class IdempotencyKeySuffixStrategy : IStrategyProvider<int>
{
    public Strategy<int> Create()
        => Strategy.Integers<int>(1, int.MaxValue);
}

public sealed class EventIngestionTestFactory : WebApplicationFactory<Program>
{
    private readonly string _tenancyDbName = $"TenancyTest_{Guid.NewGuid()}";
    private readonly string _meteringDbName = $"MeteringTest_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Use Direct strategy so tests run without an Event Hubs connection.
        builder.UseSetting("Ingestion:Strategy", "Direct");

        builder.ConfigureServices(services =>
        {
            // Replace TenancyDbContext with InMemory
            var toRemoveTenancy = services
                .Where(d =>
                    d.ServiceType.FullName?.Contains("TenancyDbContext") == true ||
                    (d.ServiceType == typeof(DbContextOptions<TenancyDbContext>)))
                .ToList();
            foreach (var d in toRemoveTenancy)
                services.Remove(d);

            services.AddDbContext<TenancyDbContext>(opts =>
                opts.UseInMemoryDatabase(_tenancyDbName));

            // Replace MeteringDbContext with InMemory
            var toRemoveMetering = services
                .Where(d =>
                    d.ServiceType.FullName?.Contains("MeteringDbContext") == true ||
                    (d.ServiceType == typeof(DbContextOptions<MeteringDbContext>)))
                .ToList();
            foreach (var d in toRemoveMetering)
                services.Remove(d);

            services.AddDbContext<MeteringDbContext>(opts =>
                opts.UseInMemoryDatabase(_meteringDbName));

            // Override ingestion strategy: use Direct so tests run without Event Hubs.
            var toRemoveStrategy = services
                .Where(d => d.ServiceType == typeof(IEventIngestionStrategy))
                .ToList();
            foreach (var d in toRemoveStrategy)
                services.Remove(d);

            services.AddScoped<IEventIngestionStrategy, DirectEventIngestionStrategy>();
        });
    }

    public async Task<(Integrator Integrator, string PlaintextSecret)> CreateIntegratorWithApiKeyAsync(
        IntegratorTier tier = IntegratorTier.Free)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

        var integrator = Integrator.Create(
            "Test Integrator",
            IntegratorKind.Production,
            tier,
            Timezone.Create("UTC"));

        var (_, secret) = integrator.MintApiKey("test-key");

        db.Integrators.Add(integrator);
        await db.SaveChangesAsync();

        return (integrator, secret);
    }
}
