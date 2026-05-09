using Saasy.SharedKernel.Domain;

namespace Saasy.SharedKernel.Domain.Tests;

public class CurrencyTests
{
    [Theory]
    [InlineData("USD", 2)]
    [InlineData("EUR", 2)]
    [InlineData("JPY", 0)]
    [InlineData("BHD", 3)]
    public void Create_ValidCode_ReturnsCurrency(string code, int minorUnits)
    {
        var currency = Currency.Create(code, minorUnits);

        Assert.Equal(code, currency.Code);
        Assert.Equal(minorUnits, currency.MinorUnits);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("123")]
    [InlineData("U1D")]
    [InlineData(null)]
    public void Create_InvalidCode_Throws(string? code)
    {
        Assert.Throws<ArgumentException>(() => Currency.Create(code!, 2));
    }

    [Fact]
    public void Create_NegativeMinorUnits_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Currency.Create("USD", -1));
    }

    [Fact]
    public void StructuralEquality_SameCodeAndMinorUnits_Equal()
    {
        var a = Currency.Create("USD", 2);
        var b = Currency.Create("USD", 2);

        Assert.Equal(a, b);
    }

    [Fact]
    public void StructuralEquality_DifferentCode_NotEqual()
    {
        var a = Currency.Create("USD", 2);
        var b = Currency.Create("EUR", 2);

        Assert.NotEqual(a, b);
    }
}
