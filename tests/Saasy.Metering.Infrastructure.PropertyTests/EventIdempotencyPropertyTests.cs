using Conjecture.Core;
using Conjecture.Xunit.V3;
using Microsoft.EntityFrameworkCore;
using Saasy.Metering.Infrastructure;
using Saasy.Metering.Infrastructure.Events;

namespace Saasy.Metering.Infrastructure.PropertyTests;

// Idempotency invariant (ADR-0014): at-least-once delivery + unique index on
// (integrator_id, idempotency_key) = effective-once storage.
//
// Commutativity property: given a batch of envelopes with arbitrary idempotency
// key collisions, any permutation of the same batch produces the same final set of
// stored rows (same cardinality, same idempotency keys).
public sealed class EventIdempotencyPropertyTests
{
    // Fixed integrator for all property test runs.
    private static readonly Guid FixedIntegratorId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    // Small key pool ensures idempotency-key collisions occur naturally.
    private static readonly IReadOnlyList<string> KeyPool = ["key-a", "key-b", "key-c", "key-d"];

    [Property]
    public void AnyPermutationOfBatch_ProducesIdenticalStoredKeySet(
        [From<KeyIndexListStrategy>] List<int> keyIndices)
    {
        // Map indices to EventRows; collisions are guaranteed when keyIndices repeats.
        var batch = keyIndices
            .Select((idx, i) => new EventRow(
                FixedIntegratorId,
                KeyPool[idx % KeyPool.Count],
                (decimal)(i + 1)))
            .ToList();

        var storedAfterOrdering1 = InsertBatch(batch);
        var reversed = Enumerable.Reverse(batch).ToList();
        var storedAfterOrdering2 = InsertBatch(reversed);

        Assert.Equal(
            storedAfterOrdering1.OrderBy(k => k),
            storedAfterOrdering2.OrderBy(k => k));
    }

    private static IReadOnlyList<string> InsertBatch(List<EventRow> batch)
    {
        var opts = new DbContextOptionsBuilder<MeteringDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new MeteringDbContext(opts);

        // In-memory provider does not enforce unique indexes. Simulate idempotency by
        // tracking (integrator_id, idempotency_key) pairs in a local set, mirroring
        // what the unique index does in Postgres.
        var seen = new HashSet<string>();

        foreach (var row in batch)
        {
            var signature = $"{row.IntegratorId}|{row.IdempotencyKey}";
            if (!seen.Add(signature))
                continue;

            db.Events.Add(MeteringEvent.Create(
                row.IntegratorId,
                customerExternalRef: "cust-001",
                eventType: "api_call",
                dimensionCode: "api_calls",
                value: row.Value,
                occurredAt: DateTimeOffset.UtcNow,
                idempotencyKey: row.IdempotencyKey,
                payload: null));
        }

        db.SaveChanges();

        return db.Events
            .Select(e => $"{e.IntegratorId}|{e.IdempotencyKey}")
            .ToList();
    }
}

internal sealed record EventRow(Guid IntegratorId, string IdempotencyKey, decimal Value);

// Generates a list of small integers (0-3) that index into the key pool.
// Uses only Strategy.Integers<int>(0, 3) and Strategy.Lists -- the simplest
// possible types, which shrink cleanly without IR fragmentation.
internal sealed class KeyIndexListStrategy : IStrategyProvider<List<int>>
{
    public Strategy<List<int>> Create()
        => Strategy.Lists(Strategy.Integers<int>(0, 3), minSize: 1, maxSize: 20);
}
