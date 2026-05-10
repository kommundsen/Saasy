using Conjecture.Core;
using Conjecture.Xunit.V3;
using Saasy.SharedKernel.Domain;
using Xunit;

namespace Saasy.SharedKernel.Domain.Tests;

public class MoneyPropertyTests
{
    [Property]
    public void Add_IsCommutative([From<SameCurrencyPairStrategy>] SameCurrencyPair pair)
    {
        Assert.Equal(pair.A.Add(pair.B), pair.B.Add(pair.A));
    }

    [Property]
    public void Add_IsAssociative([From<SameCurrencyTripleStrategy>] SameCurrencyTriple triple)
    {
        Assert.Equal(triple.A.Add(triple.B).Add(triple.C), triple.A.Add(triple.B.Add(triple.C)));
    }

    [Property]
    public void Add_ThenSubtract_ReturnsOriginal([From<SameCurrencyPairStrategy>] SameCurrencyPair pair)
    {
        Assert.Equal(pair.A, pair.A.Add(pair.B).Subtract(pair.B));
    }

    [Property]
    public void Multiply_ByOne_IsIdentity([From<MoneyStrategy>] Money a)
    {
        Assert.Equal(a, a.Multiply(1m));
    }

    [Property]
    public void Multiply_ByZero_ProducesZeroAmount([From<MoneyStrategy>] Money a)
    {
        Money result = a.Multiply(0m);
        Assert.Equal(0m, result.Amount);
        Assert.Equal(a.Currency, result.Currency);
    }

    [Property]
    public void Add_DifferentCurrencies_Throws([From<DifferentCurrencyPairStrategy>] DifferentCurrencyPair pair)
    {
        Assert.Throws<InvalidOperationException>(() => pair.A.Add(pair.B));
    }

    [Property]
    public void Subtract_DifferentCurrencies_Throws([From<DifferentCurrencyPairStrategy>] DifferentCurrencyPair pair)
    {
        Assert.Throws<InvalidOperationException>(() => pair.A.Subtract(pair.B));
    }

    // Promotion of MoneyRoundingTests.Add_PreservesDecimalPrecision.
    // Oracle: the decimal addition of the raw amounts equals the Amount on the result --
    // no implicit banker's rounding or floating-point coercion happens inside Add.
    [Property]
    public void Add_PreservesDecimalPrecision([From<SameCurrencyPairStrategy>] SameCurrencyPair pair)
    {
        decimal expected = pair.A.Amount + pair.B.Amount;

        Money result = pair.A.Add(pair.B);

        Assert.Equal(expected, result.Amount);
    }

    // Promotion of MoneyRoundingTests.Subtract_PreservesDecimalPrecision.
    // Oracle: the decimal subtraction of the raw amounts equals the Amount on the result.
    // The NonNegativeDiffPair strategy guarantees A.Amount >= B.Amount so Subtract never throws.
    [Property]
    public void Subtract_PreservesDecimalPrecision([From<NonNegativeDiffPairStrategy>] NonNegativeDiffPair pair)
    {
        decimal expected = pair.A.Amount - pair.B.Amount;

        Money result = pair.A.Subtract(pair.B);

        Assert.Equal(expected, result.Amount);
    }

    // Promotion of MoneyTests.StructuralEquality_SameAmountAndCurrency_Equal.
    // Reflexivity-by-construction: two Money values built from the same amount and currency
    // are structurally equal, for all valid (amount, currency) pairs.
    [Property]
    public void StructuralEquality_SameAmountAndCurrency_Equal([From<SameCurrencyPairStrategy>] SameCurrencyPair pair)
    {
        Money x = Money.Create(pair.A.Amount, pair.A.Currency);
        Money y = Money.Create(pair.A.Amount, pair.A.Currency);

        Assert.Equal(x, y);
    }
}
