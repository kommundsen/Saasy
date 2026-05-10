using Saasy.Tenancy.Application;
using Saasy.Tenancy.Domain;
using Saasy.Tenancy.Infrastructure.Audit;
using Microsoft.EntityFrameworkCore;

namespace Saasy.Tenancy.Infrastructure;

// CommitAsync pipeline:
// 1. Collect all domain events from EF-tracked aggregate roots.
// 2. Translate each to an AuditEntry via DomainEventAuditTranslator.
// 3. Write AuditLog rows to the DbContext.
// 4. Call SaveChangesAsync -- both domain mutations and audit rows commit in one transaction.
internal sealed class TenancyUnitOfWork(
    TenancyDbContext db,
    IAuditWriter auditWriter,
    ICurrentActor currentActor) : IUnitOfWork
{
    public async Task CommitAsync(CancellationToken ct = default)
    {
        FlushDomainEventsToAudit();
        WriteBufferedAuditEntries();
        await db.SaveChangesAsync(ct);
    }

    private void FlushDomainEventsToAudit()
    {
        var actor = currentActor.Actor;

        var aggregateRoots = db.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(e => e.Entity)
            .ToList();

        foreach (var aggregate in aggregateRoots)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                var entry = DomainEventAuditTranslator.Translate(domainEvent, actor);
                if (entry is not null)
                    auditWriter.Append(entry);
            }
            aggregate.ClearDomainEvents();
        }
    }

    private void WriteBufferedAuditEntries()
    {
        foreach (var entry in auditWriter.Flush())
        {
            db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                IntegratorId = entry.IntegratorId?.Value,
                Actor = entry.Actor,
                Action = entry.Action,
                TargetType = entry.TargetType,
                TargetId = entry.TargetId,
                PayloadJson = entry.PayloadJson,
                Timestamp = entry.Timestamp,
            });
        }
    }
}
