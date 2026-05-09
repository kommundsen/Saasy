using Conjecture.Core;
using Conjecture.Xunit.V3;
using Saasy.SharedKernel.Domain;
using Xunit;

namespace Saasy.SharedKernel.Domain.PropertyTests;

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
}
