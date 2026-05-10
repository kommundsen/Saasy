using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Domain.Tests;

public sealed class TimezoneTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    [InlineData("Eastern Standard Time")]
    [InlineData("GMT+5")]
    public void Create_WithInvalidIanaName_Throws(string? ianaName)
    {
        Assert.Throws<ArgumentException>(() => Timezone.Create(ianaName!));
    }
}
