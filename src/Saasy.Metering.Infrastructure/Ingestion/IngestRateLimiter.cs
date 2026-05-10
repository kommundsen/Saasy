using System.Collections.Concurrent;

namespace Saasy.Metering.Infrastructure.Ingestion;

// In-process fixed-window rate limiter keyed by integrator_id.
// Each entry tracks the count of ingest requests in the current 60-second window.
// Per-integrator counters are independent -- one integrator's volume does not affect another's.
//
// IMPORTANT: This counter is NOT horizontally scaled. With multiple Api instances
// the effective cap is (configuredCap * instanceCount). Distributed rate limiting
// (Redis-backed token bucket or sliding window) is required before the Http path is
// relied upon at production load with multiple replicas.
// TODO(distributed-rate-limit): replace with Redis-backed implementation.
public sealed class IngestRateLimiter
{
    private sealed record WindowEntry(long Count, DateTimeOffset WindowStart);

    private readonly ConcurrentDictionary<Guid, WindowEntry> _windows = new();
    private readonly TimeSpan _windowSize = TimeSpan.FromMinutes(1);

    // Returns true if the request is within the cap; false when the cap is exceeded.
    // capPerMinute is the caller's tier-specific value.
    public bool TryConsume(Guid integratorId, int capPerMinute)
    {
        var now = DateTimeOffset.UtcNow;

        while (true)
        {
            var existing = _windows.GetOrAdd(integratorId, _ => new WindowEntry(0, now));

            var windowStart = existing.WindowStart;
            var inCurrentWindow = now - windowStart < _windowSize;

            if (!inCurrentWindow)
            {
                // Window has expired; reset. Use CompareExchange to avoid lost updates.
                var reset = new WindowEntry(0, now);
                if (_windows.TryUpdate(integratorId, reset, existing))
                    existing = reset;
                else
                    // Another thread updated it; re-read and retry.
                    continue;
            }

            if (existing.Count >= capPerMinute)
                return false;

            var incremented = existing with { Count = existing.Count + 1 };
            if (_windows.TryUpdate(integratorId, incremented, existing))
                return true;

            // Lost the race; retry.
        }
    }

    // Seconds remaining until the current window expires for the given integrator.
    // Returns 60 when no window entry exists (worst-case retry guidance).
    public int SecondsUntilWindowReset(Guid integratorId)
    {
        if (!_windows.TryGetValue(integratorId, out var entry))
            return (int)_windowSize.TotalSeconds;

        var elapsed = DateTimeOffset.UtcNow - entry.WindowStart;
        var remaining = _windowSize - elapsed;
        return remaining > TimeSpan.Zero ? (int)Math.Ceiling(remaining.TotalSeconds) : 0;
    }
}
