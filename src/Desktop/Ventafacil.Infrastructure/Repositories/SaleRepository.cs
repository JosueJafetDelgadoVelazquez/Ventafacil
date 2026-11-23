using Microsoft.EntityFrameworkCore;
using Ventafacil.Domain.Abstractions;
using Ventafacil.Domain.Entities;
using Ventafacil.Infrastructure.Persistence;

namespace Ventafacil.Infrastructure.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public SaleRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<IReadOnlyList<Sale>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Sales
            .AsNoTracking()
            .Include(sale => sale.Items)
            .OrderByDescending(sale => sale.SoldAt)
            .ToListAsync(ct);
    }

    public async Task CompleteSaleAsync(Sale sale, CancellationToken ct = default)
    {
        if (sale.Items.Count == 0)
            throw new ArgumentException("La venta debe incluir al menos un producto.", nameof(sale));

        if (sale.Items.Any(item => item.Quantity <= 0))
            throw new ArgumentException("La cantidad de cada producto debe ser mayor que cero.", nameof(sale));

        sale.Total = sale.Items.Sum(item => item.LineTotal);
        sale.SoldAt = DateTime.Now;

        await using var db = await _factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var productIds = sale.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = await db.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, ct);

        foreach (var item in sale.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                throw new InvalidOperationException("Uno de los productos ya no está disponible.");

            if (!product.IsActive)
                throw new InvalidOperationException($"El producto {product.Name} ya no está activo.");

            if (product.Stock < item.Quantity)
                throw new InvalidOperationException(
                    $"Existencia insuficiente para {product.Name}. Disponible: {product.Stock}; solicitado: {item.Quantity}.");

            product.Stock -= item.Quantity;
            product.UpdatedAt = DateTime.UtcNow;
        }

        db.Sales.Add(sale);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
