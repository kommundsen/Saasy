using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Saasy.Tenancy.Domain.Customers;
using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Infrastructure.Customers;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new CustomerId(value))
            .HasColumnName("id");

        builder.Property(x => x.IntegratorId)
            .HasConversion(id => id.Value, value => new IntegratorId(value))
            .HasColumnName("integrator_id");

        builder.Property(x => x.ExternalRef)
            .HasColumnName("external_ref")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        builder.HasIndex(x => new { x.IntegratorId, x.ExternalRef })
            .IsUnique()
            .HasDatabaseName("IX_customers_integrator_id_external_ref");
    }
}
