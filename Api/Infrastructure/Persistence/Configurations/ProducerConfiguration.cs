using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Persistence.Configurations;

public class ProducerConfiguration : IEntityTypeConfiguration<Producer>
{
    public void Configure(EntityTypeBuilder<Producer> builder)
    {
        builder.ToTable("Producers");

        builder.HasKey(p => p.Id).HasName("PK_Producers");

        builder.Property(p => p.UserId).IsRequired();

        builder.Property(p => p.Email).IsRequired().HasMaxLength(255);

        builder.Property(p => p.CreatedAt).IsRequired();

        builder.Property(p => p.UpdatedAt);

        builder.HasIndex(p => p.Email).IsUnique().HasDatabaseName("UQ_Producers_Email");

        builder.HasIndex(p => p.UserId).IsUnique().HasDatabaseName("UQ_Producers_UserId");

        builder.HasMany(p => p.Properties)
               .WithOne(pr => pr.Producer)
               .HasForeignKey(pr => pr.ProducerId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
