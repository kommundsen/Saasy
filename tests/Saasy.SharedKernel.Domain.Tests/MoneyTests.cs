using Saasy.SharedKernel.Domain;

namespace Saasy.SharedKernel.Domain.Tests;

public class MoneyTests
{
    private static readonly Currency Usd = Currency.Create("USD", 2);
    private static readonly Currency Eur = Currency.Create("EUR", 2);

    [Fact]
    public void Create_SetsAmountAndCurrency()
    {
        var money = Money.Create(10.50m, Usd);

        Assert.Equal(10.50m, money.Amount);
        Assert.Equal(Usd, money.Currency);
    }

    [Fact]
    public void Create_ZeroAmount_IsAllowed()
    {
        var money = Money.Create(0m, Usd);

        Assert.Equal(0m, money.Amount);
    }

    [Property]
    public void Create_NegativeAmount_Throws(
        [From<NegativeDecimalStrategy>] decimal amount)
    {
        Assert.Throws<ArgumentException>(() => Money.Create(amount, Usd));
    }

    [Fact]
    public void Add_SameCurrency_ReturnsSum()
    {
        var a = Money.Create(10m, Usd);
        var b = Money.Create(5m, Usd);

        var result = a.Add(b);

        Assert.Equal(Money.Create(15m, Usd), result);
    }

    [Fact]
    public void Add_DifferentCurrency_Throws()
    {
        var a = Money.Create(10m, Usd);
        var b = Money.Create(5m, Eur);

        Assert.Throws<InvalidOperationException>(() => a.Add(b));
    }

    [Fact]
    public void Subtract_SameCurrency_ReturnsDifference()
    {
        var a = Money.Create(10m, Usd);
        var b = Money.Create(3m, Usd);

        var result = a.Subtract(b);

        Assert.Equal(Money.Create(7m, Usd), result);
    }

    [Fact]
    public void Subtract_ResultWouldBeNegative_Throws()
    {
        var a = Money.Create(3m, Usd);
        var b = Money.Create(10m, Usd);

        Assert.Throws<InvalidOperationException>(() => a.Subtract(b));
    }

    [Fact]
    public void Subtract_DifferentCurrency_Throws()
    {
        var a = Money.Create(10m, Usd);
        var b = Money.Create(3m, Eur);

        Assert.Throws<InvalidOperationException>(() => a.Subtract(b));
    }

    [Fact]
    public void Multiply_ReturnsScaledAmount()
    {
        var money = Money.Create(10m, Usd);

        var result = money.Multiply(3m);

        Assert.Equal(Money.Create(30m, Usd), result);
    }

    [Fact]
    public void Multiply_ByFraction_RetainsDecimalPrecision()
    {
        var money = Money.Create(10m, Usd);

        var result = money.Multiply(0.1m);

        Assert.Equal(Money.Create(1.0m, Usd), result);
    }

    [Fact]
    public void Multiply_NegativeFactor_Throws()
    {
        var money = Money.Create(10m, Usd);

        Assert.Throws<ArgumentException>(() => money.Multiply(-1m));
    }

}
