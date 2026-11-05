using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Ventafacil.Desktop.ViewModels;

namespace Ventafacil.Desktop.Views;

public sealed partial class ProductsPage : Page
{
    public ProductsViewModel ViewModel { get; }

    public ProductsPage()
    {
        ViewModel = App.Services.GetRequiredService<ProductsViewModel>();
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadCommand.ExecuteAsync(null);
    }

    // Funciones estáticas usadas desde x:Bind en el XAML
    public static string Money(decimal value) => value.ToString("C");
    public static string StockText(int stock) => $"Stock: {stock}";
    public static string Subtitle(string? category, string? barcode) =>
        string.Join("  ·  ", new[] { category, barcode }.Where(s => !string.IsNullOrWhiteSpace(s)));

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Eliminar producto",
            Content = $"¿Seguro que quieres eliminar \"{ViewModel.Selected?.Name}\"?",
            PrimaryButtonText = "Eliminar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            await ViewModel.DeleteCommand.ExecuteAsync(null);
    }
}

