namespace Saasy.Tenancy.Domain.Integrators;

public enum IntegratorTier
{
    Free,
    Paid,
    Enterprise
}

// Rate-limit values bound to each Integrator Tier.
// These are process-local defaults; future work will move caps to a distributed
// store (Redis-backed) so they are enforced correctly across multiple Api instances.
// TODO(distributed-rate-limit): replace in-process counter with Redis sliding window.
public static class IntegratorTierLimits
{
    // Per-minute ingest request caps per Integrator Tier.
    // Free tier: low cap; Paid: medium; Enterprise: high.
    public static int IngestRequestsPerMinute(IntegratorTier tier) => tier switch
    {
        IntegratorTier.Free => 10,
        IntegratorTier.Paid => 200,
        IntegratorTier.Enterprise => 2000,
        _ => 10
    };
}
