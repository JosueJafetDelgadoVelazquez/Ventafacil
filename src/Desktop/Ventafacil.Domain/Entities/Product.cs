namespace Ventafacil.Domain.Entities;

public class Product
{
    // Guid: permite crear productos sin conexión y sincronizarlos después sin choques de ID.
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }

    public decimal Price { get; set; }   // precio de venta
    public decimal Cost { get; set; }    // costo de compra
    public int Stock { get; set; }
    public int MinStock { get; set; }    // alerta de stock bajo

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }  // borrado lógico (útil para respaldos/sync)

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsLowStock => Stock <= MinStock;
}

