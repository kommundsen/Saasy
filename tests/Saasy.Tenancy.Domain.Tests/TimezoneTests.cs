using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Domain.Tests;

public sealed class TimezoneTests
{
    [Theory]
    [InlineData("UTC")]
    [InlineData("Europe/Oslo")]
    [InlineData("America/Los_Angeles")]
    [InlineData("Asia/Tokyo")]
    public void Create_WithValidIanaName_Succeeds(string ianaName)
    {
        var tz = Timezone.Create(ianaName);
        Assert.Equal(ianaName, tz.IanaName);
    }

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
