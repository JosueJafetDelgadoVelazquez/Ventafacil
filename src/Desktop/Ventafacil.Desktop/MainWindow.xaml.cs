using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Ventafacil.Domain.Abstractions;
using Ventafacil.Domain.Entities;

namespace Ventafacil.Desktop;

public partial class MainWindow : Window
{
    private readonly IProductRepository _productsRepository;
    private readonly PasswordStore _passwordStore;
    private readonly ObservableCollection<CartItem> _cart = [];

    public MainWindow(IProductRepository productsRepository, PasswordStore passwordStore)
    {
        InitializeComponent();
        _productsRepository = productsRepository;
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
        InventoryContentView.Visibility = Visibility.Collapsed;
        SettingsContentView.Visibility = Visibility.Collapsed;
        PointOfSaleButton.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(36, 87, 121));
        InventoryButton.Background = System.Windows.Media.Brushes.Transparent;
        SettingsButton.Background = System.Windows.Media.Brushes.Transparent;
    }

    private async void PointOfSale_Click(object sender, RoutedEventArgs e)
    {
        ShowPointOfSale();
        await RefreshProductsAsync();
    }

    private async void Inventory_Click(object sender, RoutedEventArgs e)
    {
        SalesContentView.Visibility = Visibility.Collapsed;
        InventoryContentView.Visibility = Visibility.Visible;
        SettingsContentView.Visibility = Visibility.Collapsed;
        PointOfSaleButton.Background = System.Windows.Media.Brushes.Transparent;
        InventoryButton.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(36, 87, 121));
        SettingsButton.Background = System.Windows.Media.Brushes.Transparent;
        await RefreshInventoryAsync();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        SalesContentView.Visibility = Visibility.Collapsed;
        InventoryContentView.Visibility = Visibility.Collapsed;
        SettingsContentView.Visibility = Visibility.Visible;
        PointOfSaleButton.Background = System.Windows.Media.Brushes.Transparent;
        InventoryButton.Background = System.Windows.Media.Brushes.Transparent;
        SettingsButton.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(36, 87, 121));
        PasswordSettingsStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(180, 35, 24));
        PasswordSettingsStatus.Text = string.Empty;
    }

    private async void CreateProduct_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ProductEditorWindow { Owner = this };
        if (dialog.ShowDialog() != true || dialog.CreatedProduct is not { } product)
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

    private void AddToCart_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: Product product })
            return;

        var existingItem = _cart.FirstOrDefault(item => item.ProductId == product.Id);
        if (existingItem is null)
            _cart.Add(new CartItem(product.Id, product.Name, product.Price, 1));
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
    }

    private sealed record CartItem(Guid ProductId, string Name, decimal UnitPrice, int Quantity)
    {
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
