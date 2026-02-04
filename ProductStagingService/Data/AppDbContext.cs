using Microsoft.EntityFrameworkCore;
using ProductStagingService.Models;

namespace ProductStagingService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<StagingProduct> StagingProducts => Set<StagingProduct>();

    // konfigurasi model
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StagingProduct>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.BatchId, x.RowNumber }).IsUnique();
            e.Property(x => x.Sku).HasMaxLength(64).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Price).HasPrecision(18, 2);
        });
    }
}
