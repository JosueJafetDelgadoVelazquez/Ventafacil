using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Ventafacil.Domain.Entities;

namespace Ventafacil.Desktop;

public partial class ProductEditorWindow : Window
{
    public Product? CreatedProduct { get; private set; }
    private string? _selectedImagePath;

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

        if (CategoryComboBox.SelectedItem is not System.Windows.Controls.ComboBoxItem { Content: string category })
        {
            ShowValidationError("Selecciona una categoría para el producto.", CategoryComboBox);
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

        try
        {
            var imagePath = CopySelectedImage();
            CreatedProduct = new Product
            {
                Name = name,
                Barcode = EmptyToNull(BarcodeTextBox.Text),
                Category = category,
                Price = price,
                Cost = cost,
                Stock = stock,
                MinStock = minStock,
                ImagePath = imagePath
            };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowValidationError($"No se pudo guardar la imagen.{Environment.NewLine}{exception.Message}", NameTextBox);
            return;
        }
        DialogResult = true;
    }

    private void SelectImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar imagen del producto",
            Filter = "Imágenes (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(dialog.FileName);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            _selectedImagePath = dialog.FileName;
            ProductImagePreview.Source = bitmap;
            ProductImagePreview.Visibility = Visibility.Visible;
            NoImageText.Visibility = Visibility.Collapsed;
            RemoveImageButton.Visibility = Visibility.Visible;
        }
        catch (Exception)
        {
            ShowValidationError("La imagen seleccionada no es válida.", NameTextBox);
        }
    }

    private void RemoveImage_Click(object sender, RoutedEventArgs e)
    {
        _selectedImagePath = null;
        ProductImagePreview.Source = null;
        ProductImagePreview.Visibility = Visibility.Collapsed;
        NoImageText.Visibility = Visibility.Visible;
        RemoveImageButton.Visibility = Visibility.Collapsed;
    }

    private string? CopySelectedImage()
    {
        if (string.IsNullOrWhiteSpace(_selectedImagePath))
            return null;

        var imagesFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Ventafacil",
            "Images");
        Directory.CreateDirectory(imagesFolder);

        var extension = Path.GetExtension(_selectedImagePath).ToLowerInvariant();
        var destination = Path.Combine(imagesFolder, $"{Guid.NewGuid():N}{extension}");
        File.Copy(_selectedImagePath, destination);
        return destination;
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
