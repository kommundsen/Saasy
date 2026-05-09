using Saasy.SharedKernel.Domain;

namespace Saasy.SharedKernel.Domain.Tests;

public class MoneyRoundingTests
{
    private static readonly Currency Usd = Currency.Create("USD", 2);

    [Fact]
    public void Multiply_ReturnsFullDecimalPrecision_NoImplicitRounding()
    {
        var money = new Money(1m, Usd);
        var factor = 1m / 3m;

        var result = money.Multiply(factor);

        Assert.Equal(1m * factor, result.Amount);
    }

    [Fact]
    public void Multiply_CallerCanRound_UsingDecimalRound()
    {
        var money = new Money(1m, Usd);
        var multiplied = money.Multiply(1m / 3m);
        var rounded = new Money(decimal.Round(multiplied.Amount, 2, MidpointRounding.AwayFromZero), Usd);

        Assert.Equal(new Money(0.33m, Usd), rounded);
    }

    [Fact]
    public void Add_PreservesDecimalPrecision()
    {
        var a = new Money(0.1m, Usd);
        var b = new Money(0.2m, Usd);

        var result = a.Add(b);

        Assert.Equal(0.3m, result.Amount);
    }

    [Fact]
    public void Subtract_PreservesDecimalPrecision()
    {
        var a = new Money(1.00m, Usd);
        var b = new Money(0.01m, Usd);

        var result = a.Subtract(b);

        Assert.Equal(0.99m, result.Amount);
    }
}
