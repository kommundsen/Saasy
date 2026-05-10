using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Saasy.Tenancy.Infrastructure.Audit;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_log");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.IntegratorId)
            .HasColumnName("integrator_id");

        builder.Property(x => x.Actor)
            .HasColumnName("actor")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasColumnName("action")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TargetType)
            .HasColumnName("target_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TargetId)
            .HasColumnName("target_id")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.PayloadJson)
            .HasColumnName("payload_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();

        // Audit lookups: "what happened to this Integrator?" (by integrator + time)
        // and "what happened to this entity?" (by type + id)
        builder.HasIndex(x => new { x.IntegratorId, x.Timestamp })
            .HasDatabaseName("IX_audit_log_integrator_id_timestamp");

        builder.HasIndex(x => new { x.TargetType, x.TargetId })
            .HasDatabaseName("IX_audit_log_target_type_target_id");
    }
}
