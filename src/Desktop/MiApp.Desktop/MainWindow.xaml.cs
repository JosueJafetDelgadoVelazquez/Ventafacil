using Microsoft.UI.Xaml;
using Ventafacil.Desktop.Views;

namespace Ventafacil.Desktop;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "VentaFácil";
        RootFrame.Navigate(typeof(ProductsPage));
    }
}

