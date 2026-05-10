using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Application;

public sealed record AuditEntry(
    IntegratorId? IntegratorId,
    string Actor,
    string Action,
    string TargetType,
    string TargetId,
    string PayloadJson,
    DateTimeOffset Timestamp);
