using Conjecture.Core;
using Conjecture.Xunit.V3;
using Saasy.Tenancy.Domain;
using Saasy.Tenancy.Domain.Integrators;
using Saasy.Tenancy.Infrastructure.Audit;

namespace Saasy.Tenancy.Infrastructure.Tests;

// Property tests for DomainEventAuditTranslator:
// For any sequence of N domain events on a single aggregate:
// - The produced audit-row sequence preserves order (same indices as domain events).
// - Timestamps are non-decreasing (monotonicity) within the original sequence.
// - Every audit entry references the same IntegratorId as the originating aggregate.
public sealed class AuditTranslatorPropertyTests
{
    [Property]
    public void Translate_ForSequenceOfApiKeyEvents_PreservesOrderAndReferencesOriginalIntegrator(
        [From<PositiveInt32Strategy>] int count)
    {
        var integratorId = IntegratorId.New();
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Generate N ApiKeyMinted events on the same integrator in strict time order.
        var events = Enumerable.Range(0, count)
            .Select(i => new ApiKeyMinted(
                integratorId,
                ApiKeyId.New(),
                $"key-{i}",
                "last",
                baseTime.AddSeconds(i)))
            .ToList();

        var entries = events
            .Select(e => DomainEventAuditTranslator.Translate(e, "test:actor"))
            .Where(e => e is not null)
            .Select(e => e!)
            .ToList();

        // Order preserved: each entry matches the corresponding domain event
        Assert.Equal(events.Count, entries.Count);
        for (var i = 0; i < events.Count; i++)
            Assert.Equal(events[i].ApiKeyId.Value.ToString(), entries[i].TargetId);

        // Timestamp monotonicity: each timestamp is >= the previous
        for (var i = 1; i < entries.Count; i++)
            Assert.True(entries[i].Timestamp >= entries[i - 1].Timestamp,
                $"Entry[{i}].Timestamp {entries[i].Timestamp} < Entry[{i - 1}].Timestamp {entries[i - 1].Timestamp}");

        // All entries reference the original IntegratorId
        foreach (var entry in entries)
            Assert.Equal(integratorId, entry.IntegratorId);
    }

    [Property]
    public void Translate_ForSequenceOfCustomerEvents_PreservesOrderAndReferencesOriginalIntegrator(
        [From<PositiveInt32Strategy>] int count)
    {
        var integratorId = IntegratorId.New();
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Alternate between CustomerCreated and CustomerRenamed events on distinct customers
        var customerIds = Enumerable.Range(0, count)
            .Select(_ => Saasy.Tenancy.Domain.Customers.CustomerId.New())
            .ToList();

        var events = customerIds
            .Select((cid, i) => (IDomainEvent)(i % 2 == 0
                ? new Saasy.Tenancy.Domain.Customers.CustomerCreated(
                    cid, integratorId, $"ref-{i}", $"Name-{i}", baseTime.AddSeconds(i))
                : new Saasy.Tenancy.Domain.Customers.CustomerRenamed(
                    cid, integratorId, $"Renamed-{i}", baseTime.AddSeconds(i))))
            .ToList();

        var entries = events
            .Select(e => DomainEventAuditTranslator.Translate(e, "test:actor"))
            .Where(e => e is not null)
            .Select(e => e!)
            .ToList();

        Assert.Equal(events.Count, entries.Count);

        for (var i = 1; i < entries.Count; i++)
            Assert.True(entries[i].Timestamp >= entries[i - 1].Timestamp,
                $"Entry[{i}].Timestamp not monotonic");

        foreach (var entry in entries)
            Assert.Equal(integratorId, entry.IntegratorId);
    }
}

internal sealed class PositiveInt32Strategy : IStrategyProvider<int>
{
    public Strategy<int> Create()
        => Strategy.Integers(min: 1, max: 20);
}
