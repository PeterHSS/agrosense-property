using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence.Contexts;

public sealed class AgroSenseDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Producer> Producers { get; set; }
    public DbSet<Property> Properties { get; set; }
    public DbSet<Plot> Plots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AgroSenseDbContext).Assembly);
    }
}
