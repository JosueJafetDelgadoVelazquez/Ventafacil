using System.Globalization;
using System.Windows;
using Ventafacil.Domain.Entities;

namespace Ventafacil.Desktop;

public partial class ProductEditorWindow : Window
{
    public Product? CreatedProduct { get; private set; }

    public ProductEditorWindow()
    {
        InitializeComponent();
        NameTextBox.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowValidationError("Escribe el nombre del producto.", NameTextBox);
            return;
        }

        if (!TryParseNonNegativeDecimal(PriceTextBox.Text, out var price))
        {
            ShowValidationError("Escribe un precio válido y no negativo.", PriceTextBox);
            return;
        }

        if (!TryParseNonNegativeDecimal(CostTextBox.Text, out var cost))
        {
            ShowValidationError("Escribe un costo válido y no negativo.", CostTextBox);
            return;
        }

        if (!int.TryParse(StockTextBox.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var stock) || stock < 0)
        {
            ShowValidationError("La existencia debe ser un número entero no negativo.", StockTextBox);
            return;
        }

        if (!int.TryParse(MinStockTextBox.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var minStock) || minStock < 0)
        {
            ShowValidationError("El mínimo de inventario debe ser un número entero no negativo.", MinStockTextBox);
            return;
        }

        CreatedProduct = new Product
        {
            Name = name,
            Barcode = EmptyToNull(BarcodeTextBox.Text),
            Category = EmptyToNull(CategoryTextBox.Text),
            Price = price,
            Cost = cost,
            Stock = stock,
            MinStock = minStock
        };
        DialogResult = true;
    }

    private static bool TryParseNonNegativeDecimal(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value) && value >= 0;

    private static string? EmptyToNull(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private void ShowValidationError(string message, System.Windows.Controls.Control control)
    {
        ValidationText.Text = message;
        control.Focus();
    }
}
