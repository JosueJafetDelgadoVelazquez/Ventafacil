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
        CartList.ItemsSource = _cart;
        ConfigurePasswordButton.Visibility = _passwordStore.HasPassword
            ? Visibility.Collapsed
            : Visibility.Visible;

        if (_passwordStore.HasPassword)
            ShowLogin();
        else
            ShowPointOfSale();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (PointOfSaleView.Visibility == Visibility.Visible)
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

    private void ConfigurePassword_Click(object sender, RoutedEventArgs e)
    {
        if (_passwordStore.HasPassword)
            return;

        var dialog = new PasswordSetupWindow { Owner = this };
        if (dialog.ShowDialog() != true)
            return;

        if (!_passwordStore.TrySetPassword(dialog.Password))
        {
            ConfigurePasswordButton.Visibility = Visibility.Collapsed;
            MessageBox.Show(
                "Ya existe una contraseña configurada.",
                "Contraseña",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        ConfigurePasswordButton.Visibility = Visibility.Collapsed;
        MessageBox.Show(
            "La contraseña quedó configurada. Se solicitará al iniciar VentaFácil.",
            "Contraseña configurada",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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
