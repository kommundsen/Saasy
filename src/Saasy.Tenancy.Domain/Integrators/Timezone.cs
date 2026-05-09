namespace Saasy.Tenancy.Domain.Integrators;

public sealed record Timezone
{
    public string IanaName { get; }

    private Timezone(string ianaName)
    {
        IanaName = ianaName;
    }

    public static Timezone Create(string ianaName)
    {
        if (string.IsNullOrWhiteSpace(ianaName))
            throw new ArgumentException("Timezone IANA name must not be empty.", nameof(ianaName));

        if (!IsValidIanaTimezone(ianaName))
            throw new ArgumentException(
                $"'{ianaName}' is not a valid IANA timezone name.", nameof(ianaName));

        return new Timezone(ianaName);
    }

    private static bool IsValidIanaTimezone(string ianaName)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(ianaName);

            // Windows accepts its own names (e.g. "Eastern Standard Time") via FindSystemTimeZoneById.
            // Saasy requires IANA names only. All IANA names except "UTC" contain a '/'.
            if (ianaName == "UTC")
                return true;

            return ianaName.Contains('/');
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    public override string ToString() => IanaName;
}
