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

        builder.OwnsMany(x => x.ApiKeys, apiKey =>
        {
            apiKey.ToTable("api_keys");

            apiKey.WithOwner()
                .HasForeignKey("integrator_id");

            apiKey.HasKey(k => k.Id);

            apiKey.Property(k => k.Id)
                .HasConversion(id => id.Value, value => new ApiKeyId(value))
                .HasColumnName("id");

            apiKey.Property(k => k.Name)
                .HasColumnName("name")
                .HasMaxLength(200)
                .IsRequired();

            apiKey.Property(k => k.HashedSecret)
                .HasColumnName("hashed_secret")
                .HasMaxLength(500)
                .IsRequired();

            apiKey.Property(k => k.Last4)
                .HasColumnName("last4")
                .HasMaxLength(4)
                .IsRequired();

            apiKey.Property(k => k.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            apiKey.Property(k => k.RevokedAt)
                .HasColumnName("revoked_at");
        });
    }
}
