using Saasy.Tenancy.Application;

namespace Saasy.Tenancy.Infrastructure.Audit;

// Stable identity used by Worker hosts (no HTTP context available).
internal sealed class WorkerCurrentActor : ICurrentActor
{
    public string Actor => "system:worker";
}
