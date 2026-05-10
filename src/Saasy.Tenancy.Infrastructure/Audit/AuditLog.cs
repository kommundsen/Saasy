using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Infrastructure.Audit;

// EF-tracked row for the tenancy.audit_log table.
// Not a domain aggregate -- it is infrastructure state derived from domain events.
internal sealed class AuditLog
{
    public Guid Id { get; init; }
    public Guid? IntegratorId { get; init; }
    public string Actor { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string TargetType { get; init; } = string.Empty;
    public string TargetId { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
}
