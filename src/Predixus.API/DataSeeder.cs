using Microsoft.EntityFrameworkCore;
using Predixus.Domain.Entities;
using Predixus.Infrastructure.Persistence;

namespace Predixus.API;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        if (await db.Stocks.AnyAsync())
        {
            logger.LogInformation("Seed atlandı: stocks tablosu zaten dolu.");
            return;
        }

        logger.LogInformation("BIST100 endeksi ekleniyor...");

        var xu100 = Stock.Create("XU100", "BIST 100 Endeksi", "Endeks");
        await db.Stocks.AddAsync(xu100);
        await db.SaveChangesAsync();

        logger.LogInformation("XU100 başarıyla eklendi.");
    }
}
