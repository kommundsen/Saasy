namespace Saasy.SharedKernel.Domain.Tests;

public class CurrencyTests
{
    [Fact]
    public void Create_with_valid_code_succeeds()
    {
        var currency = Currency.Create("USD");
        Assert.Equal("USD", currency.Code);
    }

    [Fact]
    public void Create_with_invalid_code_throws()
    {
        Assert.Throws<ArgumentException>(() => Currency.Create("XX"));
    }

    [Fact]
    public void Create_with_empty_code_throws()
    {
        Assert.Throws<ArgumentException>(() => Currency.Create(""));
    }

    [Fact]
    public void Create_with_lowercase_code_throws()
    {
        Assert.Throws<ArgumentException>(() => Currency.Create("usd"));
    }

    [Fact]
    public void Structural_equality_holds_for_same_code()
    {
        var a = Currency.Create("EUR");
        var b = Currency.Create("EUR");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Structural_equality_distinguishes_different_codes()
    {
        var usd = Currency.Create("USD");
        var eur = Currency.Create("EUR");
        Assert.NotEqual(usd, eur);
    }

    [Fact]
    public void MinorUnits_is_correct_for_USD()
    {
        var usd = Currency.Create("USD");
        Assert.Equal(2, usd.MinorUnits);
    }

    [Fact]
    public void MinorUnits_is_zero_for_JPY()
    {
        var jpy = Currency.Create("JPY");
        Assert.Equal(0, jpy.MinorUnits);
    }

    [Fact]
    public void ToString_returns_code()
    {
        var currency = Currency.Create("GBP");
        Assert.Equal("GBP", currency.ToString());
    }
}
