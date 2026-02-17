using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations;

public class PlotConfiguration : IEntityTypeConfiguration<Plot>
{
    public void Configure(EntityTypeBuilder<Plot> builder)
    {
        builder.ToTable("Plots");

        builder.HasKey(p => p.Id).HasName("PK_Plots");

        builder.Property(p => p.Name).HasMaxLength(100);

        builder.Property(p => p.Crop).IsRequired().HasMaxLength(100);

        builder.Property(p => p.Area).HasPrecision(10, 2).IsRequired();

        builder.Property(p => p.CreatedAt).IsRequired();

        builder.Property(p => p.UpdatedAt);

        builder.Property(p => p.PropertyId).IsRequired();

        builder.HasIndex(p => p.Id).HasDatabaseName("IX_Plots_Id");

        builder.HasIndex(p => p.PropertyId).HasDatabaseName("IX_Plots_PropertyId");
    }
}
