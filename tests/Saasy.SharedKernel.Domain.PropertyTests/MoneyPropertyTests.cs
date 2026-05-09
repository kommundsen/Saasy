using Saasy.SharedKernel.Domain;

namespace Saasy.SharedKernel.Domain.PropertyTests;

public class MoneyPropertyTests
{
    private static readonly Currency Usd = Currency.Create("USD", 2);
    private static readonly Currency Eur = Currency.Create("EUR", 2);

    // Generates pairs of non-extreme decimal amounts to keep arithmetic stable.
    public static IEnumerable<object[]> PairsOfAmounts()
    {
        var rng = new Random(42);
        for (int i = 0; i < 200; i++)
        {
            decimal a = Math.Round((decimal)(rng.NextDouble() * 10_000 - 5_000), 4);
            decimal b = Math.Round((decimal)(rng.NextDouble() * 10_000 - 5_000), 4);
            yield return [a, b];
        }
    }

    public static IEnumerable<object[]> TriplesOfAmounts()
    {
        var rng = new Random(99);
        for (int i = 0; i < 200; i++)
        {
            decimal a = Math.Round((decimal)(rng.NextDouble() * 1_000), 4);
            decimal b = Math.Round((decimal)(rng.NextDouble() * 1_000), 4);
            decimal c = Math.Round((decimal)(rng.NextDouble() * 1_000), 4);
            yield return [a, b, c];
        }
    }

    [Theory]
    [MemberData(nameof(PairsOfAmounts))]
    public void Add_IsCommutative(decimal a, decimal b)
    {
        var ma = new Money(a, Usd);
        var mb = new Money(b, Usd);

        Assert.Equal(ma.Add(mb), mb.Add(ma));
    }

    [Theory]
    [MemberData(nameof(TriplesOfAmounts))]
    public void Add_IsAssociative(decimal a, decimal b, decimal c)
    {
        var ma = new Money(a, Usd);
        var mb = new Money(b, Usd);
        var mc = new Money(c, Usd);

        Assert.Equal(ma.Add(mb).Add(mc), ma.Add(mb.Add(mc)));
    }

    [Theory]
    [MemberData(nameof(PairsOfAmounts))]
    public void Add_ThenSubtract_ReturnsOriginal(decimal a, decimal b)
    {
        var ma = new Money(a, Usd);
        var mb = new Money(b, Usd);

        Assert.Equal(ma, ma.Add(mb).Subtract(mb));
    }

    [Theory]
    [MemberData(nameof(PairsOfAmounts))]
    public void Multiply_ByOne_ReturnsIdentical(decimal a, decimal _)
    {
        var ma = new Money(a, Usd);

        Assert.Equal(ma, ma.Multiply(1m));
    }

    [Theory]
    [MemberData(nameof(PairsOfAmounts))]
    public void Multiply_ByZero_ReturnsZeroAmount(decimal a, decimal _)
    {
        var ma = new Money(a, Usd);
        var result = ma.Multiply(0m);

        Assert.Equal(0m, result.Amount);
        Assert.Equal(Usd, result.Currency);
    }

    [Theory]
    [MemberData(nameof(PairsOfAmounts))]
    public void CurrencyMismatch_Add_AlwaysThrows(decimal a, decimal b)
    {
        var ma = new Money(a, Usd);
        var mb = new Money(b, Eur);

        Assert.Throws<InvalidOperationException>((Action)(() => ma.Add(mb)));
    }

    [Theory]
    [MemberData(nameof(PairsOfAmounts))]
    public void CurrencyMismatch_Subtract_AlwaysThrows(decimal a, decimal b)
    {
        var ma = new Money(a, Usd);
        var mb = new Money(b, Eur);

        Assert.Throws<InvalidOperationException>((Action)(() => ma.Subtract(mb)));
    }
}
