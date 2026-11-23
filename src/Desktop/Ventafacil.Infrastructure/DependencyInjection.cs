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
        services.AddSingleton<ISaleRepository, SaleRepository>();
        return services;
    }

    /// <summary>Crea la base de datos local si no existe. Más adelante se reemplaza por Migrations.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider provider)
    {
        var factory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Sales" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Sales" PRIMARY KEY,
                "SoldAt" TEXT NOT NULL,
                "Total" INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS "SaleItems" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_SaleItems" PRIMARY KEY,
                "SaleId" TEXT NOT NULL,
                "ProductId" TEXT NOT NULL,
                "ProductName" TEXT NOT NULL,
                "UnitPrice" INTEGER NOT NULL,
                "Quantity" INTEGER NOT NULL,
                CONSTRAINT "FK_SaleItems_Sales_SaleId" FOREIGN KEY ("SaleId")
                    REFERENCES "Sales" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_Sales_SoldAt" ON "Sales" ("SoldAt");
            CREATE INDEX IF NOT EXISTS "IX_SaleItems_SaleId" ON "SaleItems" ("SaleId");
            """);

        // Permite actualizar bases creadas antes de que existiera la columna de imagen.
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(\"Products\")";
        await using var reader = await command.ExecuteReaderAsync();
        var hasImagePath = false;
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader["name"]?.ToString(), "ImagePath", StringComparison.OrdinalIgnoreCase))
            {
                hasImagePath = true;
                break;
            }
        }
        await reader.CloseAsync();
        await connection.CloseAsync();

        if (!hasImagePath)
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"Products\" ADD COLUMN \"ImagePath\" TEXT NULL");
    }
}

