using Conjecture.Core;
using Conjecture.Xunit.V3;
using Saasy.SharedKernel.Domain;
using Xunit;

namespace Saasy.SharedKernel.Domain.PropertyTests;

public class MoneyPropertyTests
{
    [Property(MaxExamples = 200, Seed = 42)]
    public void Add_IsCommutative([From<SameCurrencyPairStrategy>] SameCurrencyPair pair)
    {
        Assert.Equal(pair.A.Add(pair.B), pair.B.Add(pair.A));
    }

    [Property(MaxExamples = 200, Seed = 99)]
    public void Add_IsAssociative([From<SameCurrencyTripleStrategy>] SameCurrencyTriple triple)
    {
        Assert.Equal(triple.A.Add(triple.B).Add(triple.C), triple.A.Add(triple.B.Add(triple.C)));
    }

    [Property(MaxExamples = 200, Seed = 7)]
    public void Add_ThenSubtract_ReturnsOriginal([From<SameCurrencyPairStrategy>] SameCurrencyPair pair)
    {
        Assert.Equal(pair.A, pair.A.Add(pair.B).Subtract(pair.B));
    }

    [Property(MaxExamples = 200, Seed = 11)]
    public void Multiply_ByOne_IsIdentity([From<MoneyStrategy>] Money a)
    {
        Assert.Equal(a, a.Multiply(1m));
    }

    [Property(MaxExamples = 200, Seed = 13)]
    public void Multiply_ByZero_ProducesZeroAmount([From<MoneyStrategy>] Money a)
    {
        Money result = a.Multiply(0m);
        Assert.Equal(0m, result.Amount);
        Assert.Equal(a.Currency, result.Currency);
    }

    [Property(MaxExamples = 200, Seed = 17)]
    public void Add_DifferentCurrencies_Throws([From<DifferentCurrencyPairStrategy>] DifferentCurrencyPair pair)
    {
        Assert.Throws<InvalidOperationException>(() => pair.A.Add(pair.B));
    }

    [Property(MaxExamples = 200, Seed = 19)]
    public void Subtract_DifferentCurrencies_Throws([From<DifferentCurrencyPairStrategy>] DifferentCurrencyPair pair)
    {
        Assert.Throws<InvalidOperationException>(() => pair.A.Subtract(pair.B));
    }
}
