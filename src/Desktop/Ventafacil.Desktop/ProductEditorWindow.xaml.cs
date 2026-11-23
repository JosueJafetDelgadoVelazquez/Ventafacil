using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Ventafacil.Domain.Entities;

namespace Ventafacil.Desktop;

public partial class ProductEditorWindow : Window
{
    private readonly Product? _productBeingEdited;
    private string? _selectedImagePath;
    private bool _removeImage;

    public Product? SavedProduct { get; private set; }

    public ProductEditorWindow()
    {
        InitializeComponent();
        NameTextBox.Focus();
    }

    public ProductEditorWindow(Product product) : this()
    {
        _productBeingEdited = product;
        Title = "Editar producto";
        EditorTitleText.Text = "Editar producto";
        EditorDescriptionText.Text = "Actualiza los datos del producto en el inventario";
        SaveProductButton.Content = "Guardar cambios";

        NameTextBox.Text = product.Name;
        BarcodeTextBox.Text = product.Barcode ?? string.Empty;
        CategoryComboBox.Text = product.Category ?? string.Empty;
        PriceTextBox.Text = product.Price.ToString(CultureInfo.CurrentCulture);
        CostTextBox.Text = product.Cost.ToString(CultureInfo.CurrentCulture);
        StockTextBox.Text = product.Stock.ToString(CultureInfo.CurrentCulture);
        MinStockTextBox.Text = product.MinStock.ToString(CultureInfo.CurrentCulture);

        if (!string.IsNullOrWhiteSpace(product.ImagePath))
        {
            var bitmap = new ImagePathToBitmapConverter().Convert(
                product.ImagePath,
                typeof(BitmapSource),
                null,
                CultureInfo.CurrentCulture) as BitmapSource;
            if (bitmap is not null)
            {
                ProductImagePreview.Source = bitmap;
                ProductImagePreview.Visibility = Visibility.Visible;
                NoImageText.Visibility = Visibility.Collapsed;
            }

            RemoveImageButton.Visibility = Visibility.Visible;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowValidationError("Escribe el nombre del producto.", NameTextBox);
            return;
        }

        var category = CategoryComboBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(category))
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
            SavedProduct = new Product
            {
                Id = _productBeingEdited?.Id ?? Guid.NewGuid(),
                Name = name,
                Barcode = EmptyToNull(BarcodeTextBox.Text),
                Category = category,
                Price = price,
                Cost = cost,
                Stock = stock,
                MinStock = minStock,
                ImagePath = GetImagePath(),
                IsActive = _productBeingEdited?.IsActive ?? true,
                IsDeleted = _productBeingEdited?.IsDeleted ?? false,
                CreatedAt = _productBeingEdited?.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
            _removeImage = false;
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
        _removeImage = true;
        ProductImagePreview.Source = null;
        ProductImagePreview.Visibility = Visibility.Collapsed;
        NoImageText.Visibility = Visibility.Visible;
        RemoveImageButton.Visibility = Visibility.Collapsed;
    }

    private string? GetImagePath()
    {
        if (_removeImage)
            return null;

        return _selectedImagePath is null
            ? _productBeingEdited?.ImagePath
            : CopySelectedImage();
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
