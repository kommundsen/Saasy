using Conjecture.Core;

namespace Saasy.Tenancy.Domain.PropertyTests;

public sealed class ValidIanaTimezoneStrategy : IStrategyProvider<string>
{
    private static readonly IReadOnlyList<string> KnownIanaTimezones =
    [
        "UTC",
        "Europe/Oslo",
        "Europe/London",
        "Europe/Paris",
        "Europe/Berlin",
        "Europe/Amsterdam",
        "Europe/Stockholm",
        "America/New_York",
        "America/Los_Angeles",
        "America/Chicago",
        "America/Denver",
        "America/Toronto",
        "America/Sao_Paulo",
        "America/Buenos_Aires",
        "Asia/Tokyo",
        "Asia/Shanghai",
        "Asia/Singapore",
        "Asia/Kolkata",
        "Asia/Dubai",
        "Asia/Seoul",
        "Australia/Sydney",
        "Australia/Melbourne",
        "Pacific/Auckland",
        "Pacific/Honolulu",
        "Africa/Johannesburg",
        "Africa/Cairo",
    ];

    public Strategy<string> Create() => Strategy.SampledFrom(KnownIanaTimezones);
}
