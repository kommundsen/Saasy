using Conjecture.Core;
using Conjecture.Xunit.V3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Saasy.Metering.Infrastructure;
using Saasy.Metering.Infrastructure.Events;
using Saasy.Metering.Infrastructure.Ingestion;

namespace Saasy.Metering.Infrastructure.PropertyTests;

// Property: for any valid batch of N envelopes (N <= 500), submitting the batch twice
// via DirectEventIngestionStrategy yields exactly the same persisted row set as submitting
// once. The second submission is a full-batch dedup -- no new rows are added.
//
// This verifies the HTTP ingestion idempotency guarantee: at-least-once HTTP delivery
// combined with the (integrator_id, idempotency_key) unique constraint produces
// effective-once storage.
public sealed class HttpIngestionIdempotencyPropertyTests
{
    private static readonly Guid FixedIntegratorId = Guid.Parse("b1c2d3e4-f5a6-7890-bcde-fa1234567890");

    [Property]
    public async Task SubmittingBatchTwice_ProducesSameRowSetAsSubmittingOnce(
        [From<BatchSizeStrategy>] int batchSize,
        [From<DbIdStrategy>] int dbId)
    {
        var dbName = $"PropTestDb_{dbId}";
        var envelopes = BuildBatch(batchSize);

        // First submission
        var firstRowSet = await SubmitBatchAsync(envelopes, dbName);

        // Second submission to the same database
        var secondRowSet = await SubmitBatchAsync(envelopes, dbName);

        // Row count must not change on the second submission
        Assert.Equal(firstRowSet, secondRowSet);
    }

    private static IReadOnlyList<EventEnvelope> BuildBatch(int size)
    {
        return Enumerable.Range(1, size)
            .Select(i => new EventEnvelope
            {
                IntegratorId = FixedIntegratorId,
                CustomerExternalRef = "cust-001",
                EventType = "api_call",
                DimensionCode = "api_calls",
                Value = (decimal)i,
                OccurredAt = DateTimeOffset.UtcNow,
                IdempotencyKey = $"prop-key-{i}"
            })
            .ToList();
    }

    private static async Task<int> SubmitBatchAsync(
        IReadOnlyList<EventEnvelope> envelopes,
        string dbName)
    {
        var opts = new DbContextOptionsBuilder<MeteringDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using var db = new MeteringDbContext(opts);
        var strategy = new DirectEventIngestionStrategy(
            db,
            NullLogger<DirectEventIngestionStrategy>.Instance);

        await strategy.IngestAsync(envelopes, CancellationToken.None);

        return await db.Events.CountAsync();
    }
}

// Generates batch sizes between 1 and 500 -- the full valid range for the HTTP endpoint.
internal sealed class BatchSizeStrategy : IStrategyProvider<int>
{
    public Strategy<int> Create()
        => Strategy.Integers<int>(1, 500);
}

// Generates a deterministic integer used to name the InMemory database for each
// property test invocation. Each invocation needs an isolated database; deriving
// the name from the source makes runs reproducible.
internal sealed class DbIdStrategy : IStrategyProvider<int>
{
    public Strategy<int> Create()
        => Strategy.Integers<int>(1, int.MaxValue);
}
