using Microsoft.EntityFrameworkCore;
using Ventafacil.Domain.Entities;

namespace Ventafacil.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.HasKey(p => p.Id);

            e.Property(p => p.Name).IsRequired().HasMaxLength(200);
            e.Property(p => p.Barcode).HasMaxLength(64);
            e.Property(p => p.Category).HasMaxLength(100);

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
    }
}

