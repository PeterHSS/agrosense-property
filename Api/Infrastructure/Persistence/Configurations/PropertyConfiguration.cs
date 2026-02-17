using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");

        builder.HasKey(p => p.Id).HasName("PK_Properties");

        builder.Property(p => p.ProducerId).IsRequired();

        builder.Property(p => p.Name).IsRequired().HasMaxLength(500);

        builder.Property(p => p.Location).IsRequired().HasMaxLength(1000);

        builder.Property(p => p.TotalArea).IsRequired().HasPrecision(10, 2);

        builder.Property(p => p.CreatedAt).IsRequired();

        builder.Property(p => p.UpdatedAt);

        builder.HasIndex(p => p.ProducerId).HasDatabaseName("IX_Properties_ProducerId");

        builder.HasMany(p => p.Plots)
               .WithOne(pl => pl.Property)
               .HasForeignKey(pl => pl.PropertyId)
               .OnDelete(DeleteBehavior.Cascade);

    }
}
