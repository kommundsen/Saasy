using Conjecture.Core;
using Conjecture.Time;
using Conjecture.Xunit.V3;
using Saasy.Tenancy.Domain.Integrators;
using Xunit;

namespace Saasy.Tenancy.Domain.PropertyTests;

internal sealed class IanaTimezoneStrategy : IStrategyProvider<string>
{
    public Strategy<string> Create() => Strategy.IanaZoneIds(preferDst: false);
}

public class TimezonePropertyTests
{
    [Property]
    public void Create_WithValidIanaName_RoundTrips([From<IanaTimezoneStrategy>] string ianaName)
    {
        var tz = Timezone.Create(ianaName);
        Assert.Equal(ianaName, tz.IanaName);
    }
}
