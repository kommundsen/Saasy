namespace Saasy.SharedKernel.Domain;

public readonly record struct Currency
{
    public string Code { get; }
    public int MinorUnits { get; }

    private Currency(string code, int minorUnits)
    {
        Code = code;
        MinorUnits = minorUnits;
    }

    public static Currency Create(string code, int minorUnits)
    {
        if (string.IsNullOrEmpty(code) || code.Length != 3 || !IsAllLetters(code))
            throw new ArgumentException("Currency code must be a 3-letter ISO 4217 alphabetic code.", nameof(code));

        if (minorUnits < 0)
            throw new ArgumentOutOfRangeException(nameof(minorUnits), "Minor units cannot be negative.");

        return new Currency(code.ToUpperInvariant(), minorUnits);
    }

    private static bool IsAllLetters(string value)
    {
        foreach (char c in value)
        {
            if (!char.IsLetter(c))
                return false;
        }
        return true;
    }

    public override string ToString() => Code;
}
