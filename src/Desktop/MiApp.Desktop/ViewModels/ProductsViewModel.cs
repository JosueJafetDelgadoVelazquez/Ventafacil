using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ventafacil.Domain.Abstractions;
using Ventafacil.Domain.Entities;

namespace Ventafacil.Desktop.ViewModels;

public partial class ProductsViewModel : ObservableObject
{
    private readonly IProductRepository _repository;

    public ProductsViewModel(IProductRepository repository) => _repository = repository;

    public ObservableCollection<Product> Products { get; } = new();

    // ---- Estado de la lista ----
    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection), nameof(FormTitle))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private Product? _selected;

    [ObservableProperty] private string _statusMessage = string.Empty;

    public bool HasSelection => Selected is not null;
    public string FormTitle => Selected is null ? "Nuevo producto" : "Editar producto";

    // ---- Campos del formulario (NumberBox usa double) ----
    [ObservableProperty] private string _barcode = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _category = string.Empty;
    [ObservableProperty] private double _price;
    [ObservableProperty] private double _cost;
    [ObservableProperty] private double _stock;
    [ObservableProperty] private double _minStock;
    [ObservableProperty] private bool _isActive = true;

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();

    partial void OnSelectedChanged(Product? value)
    {
        if (value is null) return;

        Barcode = value.Barcode ?? string.Empty;
        Name = value.Name;
        Category = value.Category ?? string.Empty;
        Price = (double)value.Price;
        Cost = (double)value.Cost;
        Stock = value.Stock;
        MinStock = value.MinStock;
        IsActive = value.IsActive;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var selectedId = Selected?.Id;
        var items = await _repository.GetAllAsync(SearchText);

        Products.Clear();
        foreach (var p in items) Products.Add(p);

        if (selectedId is not null)
            Selected = Products.FirstOrDefault(p => p.Id == selectedId);
    }

    [RelayCommand]
    private void New()
    {
        Selected = null;
        Barcode = Name = Category = string.Empty;
        Price = Cost = Stock = MinStock = 0;
        IsActive = true;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            StatusMessage = "El nombre es obligatorio.";
            return;
        }
        if (Clean(Price) < 0 || Clean(Cost) < 0 || Clean(Stock) < 0 || Clean(MinStock) < 0)
        {
            StatusMessage = "Precio, costo y stock no pueden ser negativos.";
            return;
        }

        var product = new Product
        {
            Id = Selected?.Id ?? Guid.NewGuid(),
            CreatedAt = Selected?.CreatedAt ?? DateTime.UtcNow,
            Barcode = string.IsNullOrWhiteSpace(Barcode) ? null : Barcode.Trim(),
            Name = Name.Trim(),
            Category = string.IsNullOrWhiteSpace(Category) ? null : Category.Trim(),
            Price = (decimal)Clean(Price),
            Cost = (decimal)Clean(Cost),
            Stock = (int)Clean(Stock),
            MinStock = (int)Clean(MinStock),
            IsActive = IsActive
        };

        try
        {
            var isNew = Selected is null;
            if (isNew) await _repository.AddAsync(product);
            else await _repository.UpdateAsync(product);

            Selected = null;
            await LoadAsync();
            Selected = Products.FirstOrDefault(p => p.Id == product.Id);
            StatusMessage = isNew ? "Producto creado." : "Cambios guardados.";
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DeleteAsync()
    {
        if (Selected is null) return;

        await _repository.DeleteAsync(Selected.Id);
        New();
        await LoadAsync();
        StatusMessage = "Producto eliminado.";
    }

    // NumberBox devuelve NaN cuando el campo está vacío.
    private static double Clean(double v) => double.IsNaN(v) ? 0 : v;
}

