using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Saasy.Catalog.Domain.ProductTypes;

namespace Saasy.Catalog.Infrastructure.ProductTypes;

internal sealed class ProductTypeConfiguration : IEntityTypeConfiguration<ProductType>
{
    public void Configure(EntityTypeBuilder<ProductType> builder)
    {
        builder.ToTable("product_types");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ProductTypeId(value))
            .HasColumnName("id");

        builder.Property(x => x.IntegratorId)
            .HasConversion(id => id.Value, value => new IntegratorId(value))
            .HasColumnName("integrator_id")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        builder.OwnsMany(x => x.Dimensions, dimension =>
        {
            dimension.ToTable("dimensions");

            dimension.WithOwner()
                .HasForeignKey("product_type_id");

            dimension.HasKey(d => d.Id);

            dimension.Property(d => d.Id)
                .HasConversion(id => id.Value, value => new DimensionId(value))
                .HasColumnName("id");

            dimension.Property(d => d.Code)
                .HasColumnName("code")
                .HasMaxLength(100)
                .IsRequired();

            dimension.Property(d => d.Name)
                .HasColumnName("name")
                .HasMaxLength(200)
                .IsRequired();

            dimension.Property(d => d.Unit)
                .HasColumnName("unit")
                .HasMaxLength(100)
                .IsRequired();

            dimension.Property(d => d.Aggregation)
                .HasConversion<string>()
                .HasColumnName("aggregation")
                .HasMaxLength(30)
                .IsRequired();
        });
    }
}
