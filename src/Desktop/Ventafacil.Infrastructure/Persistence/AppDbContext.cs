using Microsoft.EntityFrameworkCore;
using Ventafacil.Domain.Entities;

namespace Ventafacil.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.HasKey(p => p.Id);

            e.Property(p => p.Name).IsRequired().HasMaxLength(200);
            e.Property(p => p.Barcode).HasMaxLength(64);
            e.Property(p => p.Category).HasMaxLength(100);
            e.Property(p => p.ImagePath).HasMaxLength(1024);

            // SQLite no tiene decimal real: se guarda en centavos (long) para evitar errores de redondeo.
            e.Property(p => p.Price).HasConversion(v => (long)Math.Round(v * 100m), v => v / 100m);
            e.Property(p => p.Cost).HasConversion(v => (long)Math.Round(v * 100m), v => v / 100m);

            e.Ignore(p => p.IsLowStock);

            // Código de barras único entre productos no eliminados.
            e.HasIndex(p => p.Barcode)
             .IsUnique()
             .HasFilter("\"Barcode\" IS NOT NULL AND \"IsDeleted\" = 0");

            e.HasIndex(p => p.Name);

            // Los productos eliminados no aparecen en ninguna consulta.
            e.HasQueryFilter(p => !p.IsDeleted);
        });

        modelBuilder.Entity<Sale>(e =>
        {
            e.ToTable("Sales");
            e.HasKey(sale => sale.Id);
            e.Property(sale => sale.Total)
                .HasConversion(value => (long)Math.Round(value * 100m), value => value / 100m);
            e.HasMany(sale => sale.Items)
                .WithOne()
                .HasForeignKey(item => item.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(sale => sale.SoldAt);
        });

        modelBuilder.Entity<SaleItem>(e =>
        {
            e.ToTable("SaleItems");
            e.HasKey(item => item.Id);
            e.Property(item => item.ProductName).IsRequired().HasMaxLength(200);
            e.Property(item => item.UnitPrice)
                .HasConversion(value => (long)Math.Round(value * 100m), value => value / 100m);
            e.Ignore(item => item.LineTotal);
        });
    }
}

