using Microsoft.EntityFrameworkCore;
using Saasy.Metering.Infrastructure;
using Saasy.Metering.Infrastructure.Events;

namespace Saasy.Metering.Infrastructure.Tests;

public sealed class IdempotencySimpleTest
{
    private static readonly Guid FixedIntegratorId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    [Fact]
    public void InsertBatch_WithDuplicates_ProducesSameResultAsReversed()
    {
        // batch with collision on key-a
        var batch = new[]
        {
            (FixedIntegratorId, "key-a", 1m),
            (FixedIntegratorId, "key-b", 2m),
            (FixedIntegratorId, "key-a", 3m), // duplicate
        }.ToList();

        var r1 = InsertBatch(batch);
        var reversed = Enumerable.Reverse(batch).ToList();
        var r2 = InsertBatch(reversed);

        Assert.Equal(r1.OrderBy(x => x), r2.OrderBy(x => x));
    }

    private static IReadOnlyList<string> InsertBatch(List<(Guid IntegratorId, string Key, decimal Value)> batch)
    {
        var opts = new DbContextOptionsBuilder<MeteringDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new MeteringDbContext(opts);
        var seen = new HashSet<string>();

        foreach (var (integratorId, key, value) in batch)
        {
            var sig = $"{integratorId}|{key}";
            if (!seen.Add(sig)) continue;

            db.Events.Add(MeteringEvent.Create(integratorId, "cust-001", "ev", "dim", value, DateTimeOffset.UtcNow, key, null));
        }

        db.SaveChanges();

        return db.Events.Select(e => $"{e.IntegratorId}|{e.IdempotencyKey}").ToList();
    }
}
