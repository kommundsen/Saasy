namespace Saasy.Tenancy.Infrastructure;

public sealed class SchemaVersion
{
    public int Id { get; private set; }
    public string MigrationId { get; private set; } = string.Empty;
    public DateTime AppliedAt { get; private set; }
}
