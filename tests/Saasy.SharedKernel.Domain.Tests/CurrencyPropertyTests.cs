namespace Saasy.SharedKernel.Domain.Tests;

public class CurrencyPropertyTests
{
    // Promotion of CurrencyTests.StructuralEquality_SameCodeAndMinorUnits_Equal.
    // Reflexivity-by-construction: two Currency values built from the same (code, minorUnits)
    // pair are structurally equal, for all entries in the ISO 4217 curated list.
    [Property]
    public void StructuralEquality_SameCodeAndMinorUnits_Equal([From<Iso4217PairStrategy>] CurrencySpec spec)
    {
        Currency a = Currency.Create(spec.Code, spec.MinorUnits);
        Currency b = Currency.Create(spec.Code, spec.MinorUnits);

        Assert.Equal(a, b);
    }
}
