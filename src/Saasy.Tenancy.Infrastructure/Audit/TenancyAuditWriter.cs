using Saasy.Tenancy.Application;

namespace Saasy.Tenancy.Infrastructure.Audit;

// Scoped per-request buffer; Flush() is called by TenancyUnitOfWork.CommitAsync before SaveChangesAsync.
internal sealed class TenancyAuditWriter : IAuditWriter
{
    private readonly List<AuditEntry> _buffer = [];

    public void Append(AuditEntry entry) => _buffer.Add(entry);

    public IReadOnlyList<AuditEntry> Flush()
    {
        var entries = _buffer.ToList();
        _buffer.Clear();
        return entries;
    }
}
