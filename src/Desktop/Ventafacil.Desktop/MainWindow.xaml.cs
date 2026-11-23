using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Ventafacil.Domain.Abstractions;
using Ventafacil.Domain.Entities;

namespace Ventafacil.Desktop;

public partial class MainWindow : Window
{
    private readonly IProductRepository _productsRepository;
    private readonly ISaleRepository _salesRepository;
    private readonly PasswordStore _passwordStore;
    private readonly ObservableCollection<CartItem> _cart = [];

    public MainWindow(
        IProductRepository productsRepository,
        ISaleRepository salesRepository,
        PasswordStore passwordStore)
    {
        InitializeComponent();
        _productsRepository = productsRepository;
        _salesRepository = salesRepository;
        _passwordStore = passwordStore;

        ProductsList.ItemsSource = new ObservableCollection<Product>();
        InventoryProductsList.ItemsSource = new ObservableCollection<Product>();
        CartList.ItemsSource = _cart;
        CurrentPasswordPanel.Visibility = _passwordStore.HasPassword
            ? Visibility.Visible
            : Visibility.Collapsed;
        PasswordSettingsDescription.Text = _passwordStore.HasPassword
            ? "Ingresa tu contraseña actual para establecer una nueva."
            : "Aún no hay una contraseña configurada. Puedes crearla aquí.";

        if (_passwordStore.HasPassword)
            ShowLogin();
        else
            ShowPointOfSale();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (SalesContentView.Visibility == Visibility.Visible)
            await RefreshProductsAsync();
    }

    private void ShowLogin()
    {
        LoginView.Visibility = Visibility.Visible;
        PointOfSaleView.Visibility = Visibility.Collapsed;
        LoginPasswordBox.Focus();
    }

    private void ShowPointOfSale()
    {
        LoginView.Visibility = Visibility.Collapsed;
        PointOfSaleView.Visibility = Visibility.Visible;
        SalesContentView.Visibility = Visibility.Visible;
        SalesHistoryContentView.Visibility = Visibility.Collapsed;
        InventoryContentView.Visibility = Visibility.Collapsed;
        SettingsContentView.Visibility = Visibility.Collapsed;
        PointOfSaleButton.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(36, 87, 121));
        SalesHistoryButton.Background = System.Windows.Media.Brushes.Transparent;
        InventoryButton.Background = System.Windows.Media.Brushes.Transparent;
        SettingsButton.Background = System.Windows.Media.Brushes.Transparent;
    }

    private async void SalesHistory_Click(object sender, RoutedEventArgs e)
    {
        SalesContentView.Visibility = Visibility.Collapsed;
        SalesHistoryContentView.Visibility = Visibility.Visible;
        InventoryContentView.Visibility = Visibility.Collapsed;
        SettingsContentView.Visibility = Visibility.Collapsed;
        PointOfSaleButton.Background = System.Windows.Media.Brushes.Transparent;
        SalesHistoryButton.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(36, 87, 121));
        InventoryButton.Background = System.Windows.Media.Brushes.Transparent;
        SettingsButton.Background = System.Windows.Media.Brushes.Transparent;
        await RefreshSalesHistoryAsync();
    }

    private async void PointOfSale_Click(object sender, RoutedEventArgs e)
    {
        ShowPointOfSale();
        await RefreshProductsAsync();
    }

    private async void Inventory_Click(object sender, RoutedEventArgs e)
    {
        SalesContentView.Visibility = Visibility.Collapsed;
        SalesHistoryContentView.Visibility = Visibility.Collapsed;
        InventoryContentView.Visibility = Visibility.Visible;
        SettingsContentView.Visibility = Visibility.Collapsed;
        PointOfSaleButton.Background = System.Windows.Media.Brushes.Transparent;
        SalesHistoryButton.Background = System.Windows.Media.Brushes.Transparent;
        InventoryButton.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(36, 87, 121));
        SettingsButton.Background = System.Windows.Media.Brushes.Transparent;
        await RefreshInventoryAsync();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        SalesContentView.Visibility = Visibility.Collapsed;
        SalesHistoryContentView.Visibility = Visibility.Collapsed;
        InventoryContentView.Visibility = Visibility.Collapsed;
        SettingsContentView.Visibility = Visibility.Visible;
        PointOfSaleButton.Background = System.Windows.Media.Brushes.Transparent;
        SalesHistoryButton.Background = System.Windows.Media.Brushes.Transparent;
        InventoryButton.Background = System.Windows.Media.Brushes.Transparent;
        SettingsButton.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(36, 87, 121));
        PasswordSettingsStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(180, 35, 24));
        PasswordSettingsStatus.Text = string.Empty;
    }

    private async void CreateInventoryReport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Guardar reporte de inventario",
            FileName = $"inventario-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            Filter = "Archivo CSV (*.csv)|*.csv",
            DefaultExt = ".csv",
            AddExtension = true
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            var products = await _productsRepository.GetAllAsync();
            var rows = new List<string>
            {
                "Nombre,Código de barras,Categoría,Precio,Costo,Existencia,Mínimo de inventario,Activo"
            };

            rows.AddRange(products.Select(product => string.Join(",",
                CsvField(product.Name),
                CsvField(product.Barcode),
                CsvField(product.Category),
                product.Price.ToString("F2", CultureInfo.InvariantCulture),
                product.Cost.ToString("F2", CultureInfo.InvariantCulture),
                product.Stock.ToString(CultureInfo.InvariantCulture),
                product.MinStock.ToString(CultureInfo.InvariantCulture),
                product.IsActive ? "Sí" : "No")));

            await File.WriteAllLinesAsync(dialog.FileName, rows, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            MessageBox.Show(
                $"El reporte se guardó correctamente en:{Environment.NewLine}{dialog.FileName}",
                "Reporte creado",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"No se pudo crear el reporte de inventario.{Environment.NewLine}{exception.Message}",
                "Error al crear reporte",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static string CsvField(string? value)
    {
        var escaped = (value ?? string.Empty).Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private async void CreateProduct_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ProductEditorWindow { Owner = this };
        if (dialog.ShowDialog() != true || dialog.SavedProduct is not { } product)
            return;

        try
        {
            await _productsRepository.AddAsync(product);
            await RefreshInventoryAsync();
            await RefreshProductsAsync();
            MessageBox.Show(
                "El producto se guardó correctamente.",
                "Producto creado",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"No se pudo guardar el producto.{Environment.NewLine}{exception.Message}",
                "Error al guardar producto",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void EditProduct_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: Product product })
            return;

        var dialog = new ProductEditorWindow(product) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.SavedProduct is not { } updatedProduct)
            return;

        try
        {
            await _productsRepository.UpdateAsync(updatedProduct);
            UpdateCartProduct(updatedProduct);
            await RefreshInventoryAsync();
            await RefreshProductsAsync();
            MessageBox.Show(
                "Los datos del producto se actualizaron correctamente.",
                "Producto actualizado",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"No se pudo actualizar el producto.{Environment.NewLine}{exception.Message}",
                "Error al actualizar producto",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: Product product })
            return;

        var confirmation = MessageBox.Show(
            this,
            $"¿Deseas eliminar el producto \"{product.Name}\"? Dejará de aparecer en el inventario.",
            "Confirmar eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirmation != MessageBoxResult.Yes)
            return;

        try
        {
            await _productsRepository.DeleteAsync(product.Id);
            var cartItem = _cart.FirstOrDefault(item => item.ProductId == product.Id);
            if (cartItem is not null)
                _cart.Remove(cartItem);
            UpdateCartSummary();
            await RefreshInventoryAsync();
            await RefreshProductsAsync();
            MessageBox.Show(
                "El producto se eliminó correctamente.",
                "Producto eliminado",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"No se pudo eliminar el producto.{Environment.NewLine}{exception.Message}",
                "Error al eliminar producto",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void UpdateCartProduct(Product product)
    {
        var cartItem = _cart.FirstOrDefault(item => item.ProductId == product.Id);
        if (cartItem is null)
            return;

        var index = _cart.IndexOf(cartItem);
        _cart[index] = cartItem with
        {
            Name = product.Name,
            UnitPrice = product.Price,
            ImagePath = product.ImagePath
        };
        UpdateCartSummary();
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_passwordStore.VerifyPassword(LoginPasswordBox.Password))
            {
                LoginStatusText.Text = string.Empty;
                LoginPasswordBox.Clear();
                ShowPointOfSale();
                await RefreshProductsAsync();
                return;
            }

            LoginPasswordBox.Clear();
            LoginStatusText.Text = "La contraseña no es correcta.";
            LoginPasswordBox.Focus();
        }
        catch (Exception exception) when (exception is IOException or System.Text.Json.JsonException)
        {
            MessageBox.Show(
                $"No se pudo verificar la contraseña configurada.{Environment.NewLine}{exception.Message}",
                "Error de acceso",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void LoginPasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            Login_Click(sender, e);
        }
    }

    private async void SearchProducts_Click(object sender, RoutedEventArgs e)
    {
        await RefreshProductsAsync();
    }

    private async void ProductSearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await RefreshProductsAsync();
        }
    }

    private async Task RefreshProductsAsync()
    {
        try
        {
            var products = (await _productsRepository.GetAllAsync(ProductSearchBox.Text))
                .Where(product => product.IsActive)
                .ToList();
            ProductsList.ItemsSource = products;
            ProductsEmptyText.Text = string.IsNullOrWhiteSpace(ProductSearchBox.Text)
                ? "No hay productos registrados todavía."
                : "No se encontraron productos.";
            ProductsEmptyText.Visibility = products.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
            SelectedProductPreview.Visibility = Visibility.Collapsed;
            ProductsList.SelectedItem = null;
        }

        catch (Exception exception)
        {
            MessageBox.Show(
                $"No se pudieron cargar los productos.{Environment.NewLine}{exception.Message}",
                "Error al cargar productos",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task RefreshInventoryAsync()
    {
        try
        {
            var products = await _productsRepository.GetAllAsync();
            InventoryProductsList.ItemsSource = products;
            InventoryEmptyText.Visibility = products.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"No se pudo cargar el inventario.{Environment.NewLine}{exception.Message}",
                "Error al cargar inventario",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task RefreshSalesHistoryAsync()
    {
        try
        {
            var sales = await _salesRepository.GetAllAsync();
            SalesHistoryList.ItemsSource = sales;
            SalesHistoryEmptyText.Visibility = sales.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"No se pudo cargar el historial de ventas.{Environment.NewLine}{exception.Message}",
                "Error al cargar historial",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void AddToCart_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: Product product })
            return;

        var existingItem = _cart.FirstOrDefault(item => item.ProductId == product.Id);
        if (existingItem is null)
            _cart.Add(new CartItem(product.Id, product.Name, product.Price, 1, product.ImagePath));
        else
        {
            var index = _cart.IndexOf(existingItem);
            _cart[index] = existingItem with { Quantity = existingItem.Quantity + 1 };
        }

        UpdateCartSummary();
    }

    private void RemoveFromCart_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: CartItem item })
            return;

        if (item.Quantity == 1)
            _cart.Remove(item);
        else
        {
            var index = _cart.IndexOf(item);
            _cart[index] = item with { Quantity = item.Quantity - 1 };
        }

        UpdateCartSummary();
    }

    private void ClearCart_Click(object sender, RoutedEventArgs e)
    {
        _cart.Clear();
        UpdateCartSummary();
    }

    private void ProductsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ProductsList.SelectedItem is not Product product)
        {
            SelectedProductPreview.Visibility = Visibility.Collapsed;
            return;
        }

        var converter = new ImagePathToBitmapConverter();
        SelectedProductImage.Source = converter.Convert(
            product.ImagePath,
            typeof(System.Windows.Media.Imaging.BitmapSource),
            null,
            CultureInfo.CurrentCulture) as System.Windows.Media.Imaging.BitmapSource;
        SelectedProductName.Text = product.Name;
        SelectedProductPrice.Text = product.Price.ToString("C2", CultureInfo.GetCultureInfo("es-MX"));
        SelectedProductStock.Text = $"Existencia: {product.Stock}";
        SelectedProductPreview.Visibility = Visibility.Visible;
    }

    private async void Sell_Click(object sender, RoutedEventArgs e)
    {
        if (_cart.Count == 0)
            return;

        var total = _cart.Sum(item => item.LineTotal);
        var confirmation = MessageBox.Show(
            this,
            $"¿Confirmas la venta por {total.ToString("C2", CultureInfo.GetCultureInfo("es-MX"))}?",
            "Confirmar venta",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirmation != MessageBoxResult.Yes)
            return;

        var sale = new Sale
        {
            Total = total,
            Items = _cart.Select(item => new SaleItem
            {
                ProductId = item.ProductId,
                ProductName = item.Name,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity
            }).ToList()
        };
        try
        {
            await _salesRepository.CompleteSaleAsync(sale);
            _cart.Clear();
            UpdateCartSummary();
            await RefreshProductsAsync();
            await RefreshInventoryAsync();
            MessageBox.Show(
                this,
                $"Venta completada por {total.ToString("C2", CultureInfo.GetCultureInfo("es-MX"))}. El inventario se actualizó.",
                "Venta completada",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"No se pudo completar la venta.{Environment.NewLine}{exception.Message}",
                "Error al vender",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void SavePasswordSettings_Click(object sender, RoutedEventArgs e)
    {
        PasswordSettingsStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(180, 35, 24));

        if (NewPasswordBox.Password.Length < 6)
        {
            PasswordSettingsStatus.Text = "La contraseña debe tener al menos 6 caracteres.";
            return;
        }

        if (!string.Equals(NewPasswordBox.Password, ConfirmNewPasswordBox.Password, StringComparison.Ordinal))
        {
            PasswordSettingsStatus.Text = "Las contraseñas no coinciden.";
            ConfirmNewPasswordBox.Clear();
            ConfirmNewPasswordBox.Focus();
            return;
        }

        try
        {
            var saved = _passwordStore.HasPassword
                ? _passwordStore.TryChangePassword(CurrentPasswordBox.Password, NewPasswordBox.Password)
                : _passwordStore.TrySetPassword(NewPasswordBox.Password);
            if (!saved)
            {
                PasswordSettingsStatus.Text = _passwordStore.HasPassword
                    ? "La contraseña actual no es correcta."
                    : "No se pudo guardar la contraseña. Inténtalo de nuevo.";
                CurrentPasswordBox.Clear();
                CurrentPasswordBox.Focus();
                return;
            }

            CurrentPasswordPanel.Visibility = Visibility.Visible;
            PasswordSettingsDescription.Text = "Ingresa tu contraseña actual para establecer una nueva.";
            CurrentPasswordBox.Clear();
            NewPasswordBox.Clear();
            ConfirmNewPasswordBox.Clear();
            PasswordSettingsStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(22, 101, 52));
            PasswordSettingsStatus.Text = "La contraseña se guardó correctamente.";
        }
        catch (Exception exception) when (exception is IOException or System.Text.Json.JsonException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"No se pudo guardar la contraseña.{Environment.NewLine}{exception.Message}",
                "Error de configuración",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void UpdateCartSummary()
    {
        var total = _cart.Sum(item => item.LineTotal);
        CartTotalText.Text = total.ToString("C2", CultureInfo.GetCultureInfo("es-MX"));
        CartStatusText.Text = _cart.Count == 0
            ? "El carrito está vacío."
            : $"{_cart.Sum(item => item.Quantity)} artículo(s) en el carrito.";
        SellButton.IsEnabled = _cart.Count > 0;
    }

    private sealed record CartItem(Guid ProductId, string Name, decimal UnitPrice, int Quantity, string? ImagePath)
    {
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
