using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Infrastructure.Integrators;

internal sealed class IntegratorConfiguration : IEntityTypeConfiguration<Integrator>
{
    public void Configure(EntityTypeBuilder<Integrator> builder)
    {
        builder.ToTable("integrators");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new IntegratorId(value))
            .HasColumnName("id");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Kind)
            .HasConversion<string>()
            .HasColumnName("kind")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Tier)
            .HasConversion<string>()
            .HasColumnName("tier")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Timezone)
            .HasConversion(
                tz => tz.IanaName,
                raw => Timezone.Create(raw))
            .HasColumnName("timezone")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();
    }
}
