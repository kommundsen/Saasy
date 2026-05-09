namespace Saasy.SharedKernel.Domain;

public sealed record Currency
{
    public string Code { get; }
    public int MinorUnits { get; }

    private Currency(string code, int minorUnits)
    {
        Code = code;
        MinorUnits = minorUnits;
    }

    public static Currency Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Currency code must not be empty.", nameof(code));

        if (code.Length != 3 || code != code.ToUpperInvariant())
            throw new ArgumentException($"'{code}' is not a valid ISO 4217 currency code.", nameof(code));

        if (!Iso4217.TryGet(code, out var minorUnits))
            throw new ArgumentException($"'{code}' is not a recognised ISO 4217 currency code.", nameof(code));

        return new Currency(code, minorUnits);
    }

    public override string ToString() => Code;
}
