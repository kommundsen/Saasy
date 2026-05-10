using Saasy.SharedKernel.Domain;

namespace Saasy.SharedKernel.Domain.Tests;

public class MoneyRoundingTests
{
    private static readonly Currency Usd = Currency.Create("USD", 2);

    [Fact]
    public void Multiply_ReturnsFullDecimalPrecision_NoImplicitRounding()
    {
        var money = Money.Create(1m, Usd);
        var factor = 1m / 3m;

        var result = money.Multiply(factor);

        Assert.Equal(1m * factor, result.Amount);
    }

    [Fact]
    public void Multiply_CallerCanRound_UsingDecimalRound()
    {
        var money = Money.Create(1m, Usd);
        var multiplied = money.Multiply(1m / 3m);
        var rounded = Money.Create(decimal.Round(multiplied.Amount, 2, MidpointRounding.AwayFromZero), Usd);

        Assert.Equal(Money.Create(0.33m, Usd), rounded);
    }

}
