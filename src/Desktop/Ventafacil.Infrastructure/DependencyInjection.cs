using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ventafacil.Domain.Abstractions;
using Ventafacil.Infrastructure.Persistence;
using Ventafacil.Infrastructure.Repositories;

namespace Ventafacil.Infrastructure;

public static class DependencyInjection
{
    public static string GetDatabasePath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Ventafacil");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "ventafacil.db");
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContextFactory<AppDbContext>(o =>
            o.UseSqlite($"Data Source={GetDatabasePath()}"));

        services.AddSingleton<IProductRepository, ProductRepository>();
        return services;
    }

    /// <summary>Crea la base de datos local si no existe. Más adelante se reemplaza por Migrations.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider provider)
    {
        var factory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
    }
}

