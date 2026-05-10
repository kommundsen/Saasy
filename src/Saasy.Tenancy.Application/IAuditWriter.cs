namespace Saasy.Tenancy.Application;

public interface IAuditWriter
{
    void Append(AuditEntry entry);
    IReadOnlyList<AuditEntry> Flush();
}
