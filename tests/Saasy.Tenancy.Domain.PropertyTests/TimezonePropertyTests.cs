using Conjecture.Core;
using Conjecture.Xunit.V3;
using Saasy.Tenancy.Domain.Integrators;
using Xunit;

namespace Saasy.Tenancy.Domain.PropertyTests;

public class TimezonePropertyTests
{
    [Property]
    public void Create_WithValidIanaName_RoundTrips([From<ValidIanaTimezoneStrategy>] string ianaName)
    {
        var tz = Timezone.Create(ianaName);
        Assert.Equal(ianaName, tz.IanaName);
    }
}
