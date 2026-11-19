using Microsoft.EntityFrameworkCore;
using Ventafacil.Domain.Abstractions;
using Ventafacil.Domain.Entities;
using Ventafacil.Infrastructure.Persistence;

namespace Ventafacil.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public ProductRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<IReadOnlyList<Product>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        IQueryable<Product> query = db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.Like(p.Name, pattern) ||
                (p.Barcode != null && EF.Functions.Like(p.Barcode, pattern)) ||
                (p.Category != null && EF.Functions.Like(p.Category, pattern)));
        }

        return await query.OrderBy(p => p.Name).ToListAsync(ct);
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await EnsureBarcodeIsUniqueAsync(db, product, ct);

        product.CreatedAt = product.UpdatedAt = DateTime.UtcNow;
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var current = await db.Products.FirstOrDefaultAsync(p => p.Id == product.Id, ct)
                      ?? throw new InvalidOperationException("El producto ya no existe.");

        await EnsureBarcodeIsUniqueAsync(db, product, ct);

        current.Barcode = product.Barcode;
        current.Name = product.Name;
        current.Category = product.Category;
        current.Price = product.Price;
        current.Cost = product.Cost;
        current.Stock = product.Stock;
        current.MinStock = product.MinStock;
        current.ImagePath = product.ImagePath;
        current.IsActive = product.IsActive;
        current.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    public async Task DeductStockAsync(IReadOnlyDictionary<Guid, int> quantities, CancellationToken ct = default)
    {
        if (quantities.Count == 0)
            throw new ArgumentException("La venta debe incluir al menos un producto.", nameof(quantities));

        if (quantities.Any(item => item.Value <= 0))
            throw new ArgumentException("La cantidad de cada producto debe ser mayor que cero.", nameof(quantities));

        await using var db = await _factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var products = await db.Products
            .Where(product => quantities.Keys.Contains(product.Id))
            .ToListAsync(ct);

        foreach (var (productId, quantity) in quantities)
        {
            var product = products.FirstOrDefault(item => item.Id == productId)
                          ?? throw new InvalidOperationException("Uno de los productos ya no está disponible.");

            if (!product.IsActive)
                throw new InvalidOperationException($"El producto {product.Name} ya no está activo.");

            if (product.Stock < quantity)
                throw new InvalidOperationException(
                    $"Existencia insuficiente para {product.Name}. Disponible: {product.Stock}; solicitado: {quantity}.");

            product.Stock -= quantity;
            product.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var current = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (current is null) return;

        current.IsDeleted = true;          // borrado lógico
        current.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureBarcodeIsUniqueAsync(AppDbContext db, Product product, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(product.Barcode)) return;

        var exists = await db.Products.AnyAsync(
            p => p.Barcode == product.Barcode && p.Id != product.Id, ct);

        if (exists)
            throw new InvalidOperationException($"Ya existe un producto con el código de barras {product.Barcode}.");
    }
}

