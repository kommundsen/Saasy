using Conjecture.Core;
using Conjecture.Money;
using Saasy.SharedKernel.Domain;

namespace Saasy.SharedKernel.Domain.Tests;

public readonly record struct SameCurrencyPair(Money A, Money B);

// A pair where A.Amount >= B.Amount, so Subtract never throws.
public readonly record struct NonNegativeDiffPair(Money A, Money B);

public readonly record struct SameCurrencyTriple(Money A, Money B, Money C);
public readonly record struct DifferentCurrencyPair(Money A, Money B);

// A (code, minorUnits) pair drawn from a curated ISO 4217 list.
public readonly record struct CurrencySpec(string Code, int MinorUnits);

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
            .Select(amount => Money.Create(amount, currency));
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
                return new SameCurrencyPair(Money.Create(amount1, currency), Money.Create(amount2, currency));
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
                    Money.Create(amount1, currency),
                    Money.Create(amount2, currency),
                    Money.Create(amount3, currency));
            });

    public Strategy<SameCurrencyTriple> Create() => Inner;
}

public sealed class NegativeDecimalStrategy : IStrategyProvider<decimal>
{
    public Strategy<decimal> Create() => Strategy.Decimals(min: -10_000m, max: -0.01m);
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

public sealed class NonNegativeDiffPairStrategy : IStrategyProvider<NonNegativeDiffPair>
{
    // Generates a same-currency pair where A.Amount >= B.Amount so that
    // A.Subtract(B) never throws an InvalidOperationException.
    private static readonly Strategy<NonNegativeDiffPair> Inner =
        Strategy.Tuples(
                MoneyStrategy.Currencies,
                Strategy.Amounts("USD", min: 0m, max: 10_000m),
                Strategy.Amounts("USD", min: 0m, max: 10_000m))
            .Select(t =>
            {
                Currency currency = t.Item1;
                decimal raw1 = Math.Round(t.Item2, currency.MinorUnits, MidpointRounding.ToEven);
                decimal raw2 = Math.Round(t.Item3, currency.MinorUnits, MidpointRounding.ToEven);
                // Ensure A >= B by ordering.
                decimal bigger = Math.Max(raw1, raw2);
                decimal smaller = Math.Min(raw1, raw2);
                return new NonNegativeDiffPair(
                    Money.Create(bigger, currency),
                    Money.Create(smaller, currency));
            });

    public Strategy<NonNegativeDiffPair> Create() => Inner;
}

public sealed class Iso4217PairStrategy : IStrategyProvider<CurrencySpec>
{
    // Curated list of ISO 4217 codes with their standard minor units.
    private static readonly IReadOnlyList<CurrencySpec> KnownCurrencies =
    [
        new("USD", 2),
        new("EUR", 2),
        new("GBP", 2),
        new("JPY", 0),
        new("BHD", 3),
        new("NOK", 2),
        new("CHF", 2),
        new("CAD", 2),
        new("AUD", 2),
        new("KWD", 3),
    ];

    public Strategy<CurrencySpec> Create() => Strategy.SampledFrom(KnownCurrencies);
}
