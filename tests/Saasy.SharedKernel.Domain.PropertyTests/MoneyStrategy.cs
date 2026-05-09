using Conjecture.Core;
using Conjecture.Money;
using Saasy.SharedKernel.Domain;

namespace Saasy.SharedKernel.Domain.PropertyTests;

public readonly record struct SameCurrencyPair(Money A, Money B);
public readonly record struct SameCurrencyTriple(Money A, Money B, Money C);
public readonly record struct DifferentCurrencyPair(Money A, Money B);

public sealed class MoneyStrategy : IStrategyProvider<Money>
{
    private static readonly IReadOnlyList<Currency> SupportedCurrencies =
    [
        Currency.Create("USD", 2),
        Currency.Create("EUR", 2),
        Currency.Create("GBP", 2),
        Currency.Create("JPY", 0),
    ];

    internal static readonly Strategy<Currency> Currencies =
        Strategy.SampledFrom(SupportedCurrencies);

    internal static Strategy<Money> ForCurrency(Currency currency)
    {
        return Strategy.Amounts(currency.Code, min: 0m, max: 10_000m)
            .Select(amount => new Money(amount, currency));
    }

    public Strategy<Money> Create()
    {
        return Currencies.SelectMany(ForCurrency);
    }
}

public sealed class SameCurrencyPairStrategy : IStrategyProvider<SameCurrencyPair>
{
    // Amounts are generated as USD (scale 2) then rounded to the actual currency's
    // minor units inside Select, keeping IR consumption uniform across all currencies.
    // This avoids "Replay IR exhausted" in Conjecture's failure message formatter.
    private static readonly Strategy<SameCurrencyPair> Inner =
        Strategy.Tuples(
                MoneyStrategy.Currencies,
                Strategy.Amounts("USD", min: 0m, max: 10_000m),
                Strategy.Amounts("USD", min: 0m, max: 10_000m))
            .Select(t =>
            {
                Currency currency = t.Item1;
                decimal amount1 = Math.Round(t.Item2, currency.MinorUnits, MidpointRounding.ToEven);
                decimal amount2 = Math.Round(t.Item3, currency.MinorUnits, MidpointRounding.ToEven);
                return new SameCurrencyPair(new Money(amount1, currency), new Money(amount2, currency));
            });

    public Strategy<SameCurrencyPair> Create() => Inner;
}

public sealed class SameCurrencyTripleStrategy : IStrategyProvider<SameCurrencyTriple>
{
    // Same uniform-IR-consumption approach as SameCurrencyPairStrategy.
    private static readonly Strategy<SameCurrencyTriple> Inner =
        Strategy.Tuples(
                MoneyStrategy.Currencies,
                Strategy.Amounts("USD", min: 0m, max: 10_000m),
                Strategy.Amounts("USD", min: 0m, max: 10_000m),
                Strategy.Amounts("USD", min: 0m, max: 10_000m))
            .Select(t =>
            {
                Currency currency = t.Item1;
                decimal amount1 = Math.Round(t.Item2, currency.MinorUnits, MidpointRounding.ToEven);
                decimal amount2 = Math.Round(t.Item3, currency.MinorUnits, MidpointRounding.ToEven);
                decimal amount3 = Math.Round(t.Item4, currency.MinorUnits, MidpointRounding.ToEven);
                return new SameCurrencyTriple(
                    new Money(amount1, currency),
                    new Money(amount2, currency),
                    new Money(amount3, currency));
            });

    public Strategy<SameCurrencyTriple> Create() => Inner;
}

public sealed class DifferentCurrencyPairStrategy : IStrategyProvider<DifferentCurrencyPair>
{
    public Strategy<DifferentCurrencyPair> Create()
    {
        return Strategy.Compose(ctx =>
        {
            Currency currencyA = ctx.Generate(MoneyStrategy.Currencies);
            Currency currencyB = ctx.Generate(MoneyStrategy.Currencies);
            ctx.Assume(currencyA != currencyB);
            Money a = ctx.Generate(MoneyStrategy.ForCurrency(currencyA));
            Money b = ctx.Generate(MoneyStrategy.ForCurrency(currencyB));
            return new DifferentCurrencyPair(a, b);
        });
    }
}
