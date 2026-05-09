namespace Saasy.SharedKernel.Domain.Tests;

public class MoneyTests
{
    private static readonly Currency Usd = Currency.Create("USD");
    private static readonly Currency Eur = Currency.Create("EUR");
    private static readonly Currency Jpy = Currency.Create("JPY");

    [Fact]
    public void Create_with_zero_amount_succeeds()
    {
        var money = Money.Create(0m, Usd);
        Assert.Equal(0m, money.Amount);
        Assert.Equal(Usd, money.Currency);
    }

    [Fact]
    public void Create_with_positive_amount_succeeds()
    {
        var money = Money.Create(9.99m, Usd);
        Assert.Equal(9.99m, money.Amount);
    }

    [Fact]
    public void Create_with_negative_amount_throws()
    {
        Assert.Throws<ArgumentException>(() => Money.Create(-0.01m, Usd));
    }

    [Fact]
    public void Add_same_currency_returns_sum()
    {
        var a = Money.Create(10m, Usd);
        var b = Money.Create(5m, Usd);
        var result = a.Add(b);
        Assert.Equal(15m, result.Amount);
        Assert.Equal(Usd, result.Currency);
    }

    [Fact]
    public void Add_currency_mismatch_throws()
    {
        var a = Money.Create(10m, Usd);
        var b = Money.Create(10m, Eur);
        Assert.Throws<InvalidOperationException>(() => a.Add(b));
    }

    [Fact]
    public void Subtract_same_currency_returns_difference()
    {
        var a = Money.Create(10m, Usd);
        var b = Money.Create(3m, Usd);
        var result = a.Subtract(b);
        Assert.Equal(7m, result.Amount);
        Assert.Equal(Usd, result.Currency);
    }

    [Fact]
    public void Subtract_currency_mismatch_throws()
    {
        var a = Money.Create(10m, Usd);
        var b = Money.Create(5m, Eur);
        Assert.Throws<InvalidOperationException>((Action)(() => a.Subtract(b)));
    }

    [Fact]
    public void Subtract_below_zero_throws()
    {
        var a = Money.Create(5m, Usd);
        var b = Money.Create(10m, Usd);
        Assert.Throws<InvalidOperationException>((Action)(() => a.Subtract(b)));
    }

    [Fact]
    public void Multiply_by_positive_factor_succeeds()
    {
        var money = Money.Create(3m, Usd);
        var result = money.Multiply(4m);
        Assert.Equal(12m, result.Amount);
        Assert.Equal(Usd, result.Currency);
    }

    [Fact]
    public void Multiply_by_zero_returns_zero()
    {
        var money = Money.Create(9.99m, Usd);
        var result = money.Multiply(0m);
        Assert.Equal(0m, result.Amount);
    }

    [Fact]
    public void Multiply_by_negative_factor_throws()
    {
        var money = Money.Create(5m, Usd);
        Assert.Throws<ArgumentException>((Action)(() => money.Multiply(-1m)));
    }

    [Fact]
    public void Round_applies_midpoint_rounding()
    {
        var money = Money.Create(1.005m, Usd);
        var rounded = money.Round(MidpointRounding.AwayFromZero);
        Assert.Equal(1.01m, rounded.Amount);
        Assert.Equal(Usd, rounded.Currency);
    }

    [Fact]
    public void Round_to_zero_minor_units_for_JPY()
    {
        var money = Money.Create(100.7m, Jpy);
        var rounded = money.Round(MidpointRounding.AwayFromZero);
        Assert.Equal(101m, rounded.Amount);
    }

    [Fact]
    public void Structural_equality_holds_for_same_amount_and_currency()
    {
        var a = Money.Create(10m, Usd);
        var b = Money.Create(10m, Usd);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Structural_equality_distinguishes_different_currency()
    {
        var a = Money.Create(10m, Usd);
        var b = Money.Create(10m, Eur);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Structural_equality_distinguishes_different_amount()
    {
        var a = Money.Create(10m, Usd);
        var b = Money.Create(11m, Usd);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Zero_returns_zero_money_for_currency()
    {
        var zero = Money.Zero(Usd);
        Assert.Equal(0m, zero.Amount);
        Assert.Equal(Usd, zero.Currency);
    }
}
