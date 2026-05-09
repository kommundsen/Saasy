using Saasy.SharedKernel.Domain;

namespace Saasy.SharedKernel.Domain.Tests;

public class MoneyTests
{
    private static readonly Currency Usd = Currency.Create("USD", 2);
    private static readonly Currency Eur = Currency.Create("EUR", 2);

    [Fact]
    public void Constructor_SetsAmountAndCurrency()
    {
        var money = new Money(10.50m, Usd);

        Assert.Equal(10.50m, money.Amount);
        Assert.Equal(Usd, money.Currency);
    }

    [Fact]
    public void Add_SameCurrency_ReturnsSum()
    {
        var a = new Money(10m, Usd);
        var b = new Money(5m, Usd);

        var result = a.Add(b);

        Assert.Equal(new Money(15m, Usd), result);
    }

    [Fact]
    public void Add_DifferentCurrency_Throws()
    {
        var a = new Money(10m, Usd);
        var b = new Money(5m, Eur);

        Assert.Throws<InvalidOperationException>((Action)(() => a.Add(b)));
    }

    [Fact]
    public void Subtract_SameCurrency_ReturnsDifference()
    {
        var a = new Money(10m, Usd);
        var b = new Money(3m, Usd);

        var result = a.Subtract(b);

        Assert.Equal(new Money(7m, Usd), result);
    }

    [Fact]
    public void Subtract_DifferentCurrency_Throws()
    {
        var a = new Money(10m, Usd);
        var b = new Money(3m, Eur);

        Assert.Throws<InvalidOperationException>((Action)(() => a.Subtract(b)));
    }

    [Fact]
    public void Multiply_ReturnsScaledAmount()
    {
        var money = new Money(10m, Usd);

        var result = money.Multiply(3m);

        Assert.Equal(new Money(30m, Usd), result);
    }

    [Fact]
    public void Multiply_ByFraction_RetainsDecimalPrecision()
    {
        var money = new Money(10m, Usd);

        var result = money.Multiply(0.1m);

        Assert.Equal(new Money(1.0m, Usd), result);
    }

    [Fact]
    public void StructuralEquality_SameAmountAndCurrency_Equal()
    {
        var a = new Money(10m, Usd);
        var b = new Money(10m, Usd);

        Assert.Equal(a, b);
    }

    [Fact]
    public void StructuralEquality_DifferentAmount_NotEqual()
    {
        var a = new Money(10m, Usd);
        var b = new Money(11m, Usd);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void StructuralEquality_DifferentCurrency_NotEqual()
    {
        var a = new Money(10m, Usd);
        var b = new Money(10m, Eur);

        Assert.NotEqual(a, b);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5.00)]
    [InlineData(0.01)]
    public void Zero_And_NegativeAmounts_AllowedInConstruction(double rawAmount)
    {
        var amount = (decimal)rawAmount;
        var money = new Money(amount, Usd);

        Assert.Equal(amount, money.Amount);
    }
}
