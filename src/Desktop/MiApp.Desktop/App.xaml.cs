using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Ventafacil.Desktop.ViewModels;
using Ventafacil.Infrastructure;

namespace Ventafacil.Desktop;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private Window? _window;

    public App()
    {
        InitializeComponent();

        var services = new ServiceCollection();
        services.AddInfrastructure();
        services.AddTransient<ProductsViewModel>();
        Services = services.BuildServiceProvider();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        await Services.InitializeDatabaseAsync();   // crea ventafacil.db la primera vez

        _window = new MainWindow();
        _window.Activate();
    }
}

