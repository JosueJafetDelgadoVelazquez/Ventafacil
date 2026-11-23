namespace Ventafacil.Domain.Entities;

public class Sale
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime SoldAt { get; set; } = DateTime.Now;
    public decimal Total { get; set; }
    public List<SaleItem> Items { get; set; } = [];
}
