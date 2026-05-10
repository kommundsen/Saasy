using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Saasy.Metering.Infrastructure.Events;

internal sealed class MeteringEventConfiguration : IEntityTypeConfiguration<MeteringEvent>
{
    public void Configure(EntityTypeBuilder<MeteringEvent> builder)
    {
        builder.ToTable("events");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.IntegratorId)
            .HasColumnName("integrator_id")
            .IsRequired();

        builder.Property(x => x.CustomerExternalRef)
            .HasColumnName("customer_external_ref")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DimensionCode)
            .HasColumnName("dimension_code")
            .HasMaxLength(200)
            .IsRequired();

        // decimal -- exact arithmetic for usage values feeding monetary calculations.
        builder.Property(x => x.Value)
            .HasColumnName("value")
            .HasColumnType("numeric")
            .IsRequired();

        builder.Property(x => x.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.Property(x => x.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(500)
            .IsRequired();

        // JsonDocument does not have a built-in EF Core type mapping.
        // The value converter serialises to/from a JSON string so that:
        //   - Postgres stores it as JSONB (via the column type annotation).
        //   - The EF InMemory provider (used in tests) stores it as a string.
        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : v.RootElement.GetRawText(),
                v => v == null ? null : JsonDocument.Parse(v));

        builder.Property(x => x.IngestedAt)
            .HasColumnName("ingested_at")
            .IsRequired();

        // Unique index enforces effective-once semantics across retries.
        builder.HasIndex(x => new { x.IntegratorId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("IX_events_integrator_id_idempotency_key");
    }
}
