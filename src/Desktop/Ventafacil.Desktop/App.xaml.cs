using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Ventafacil.Infrastructure;

namespace Ventafacil.Desktop;

public partial class App : Application
{
    private ServiceProvider? _services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _services = new ServiceCollection()
                .AddInfrastructure()
                .AddSingleton<PasswordStore>()
                .AddTransient<MainWindow>()
                .BuildServiceProvider();

            await _services.InitializeDatabaseAsync();
            _services.GetRequiredService<MainWindow>().Show();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"No se pudo iniciar VentaFácil.{Environment.NewLine}{exception.Message}",
                "Error al iniciar",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }
}
