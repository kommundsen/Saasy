namespace Saasy.Tenancy.Infrastructure.Persistence;

internal sealed class MigrationSentinel
{
    public int Id { get; set; }
    public DateTime AppliedAt { get; set; }
}
