namespace Saasy.Metering.Infrastructure.Tests;

// Generates representative blank strings: null, empty, whitespace-only.
// "Blank" means string.IsNullOrWhiteSpace returns true.
internal sealed class BlankStringStrategy : IStrategyProvider<string?>
{
    public Strategy<string?> Create()
        => Strategy.SampledFrom<string?>(new string?[] { null, "", " ", "\t", "\n", "   ", " \t " });
}
