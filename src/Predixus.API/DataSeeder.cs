using Microsoft.EntityFrameworkCore;
using Predixus.Application.Interfaces;
using Predixus.Domain.Entities;
using Predixus.Infrastructure.Persistence;

namespace Predixus.API;

public static class DataSeeder
{
    private const string AdminEmail = "admin@predixus.com";
    private const string AdminPassword = "Admin123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await SeedStocksAsync(db, logger);
        await SeedAdminAsync(db, logger, passwordHasher);
    }

    private static async Task SeedStocksAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Stocks.AnyAsync()) return;

        logger.LogInformation("BIST100 endeksi ekleniyor...");
        var xu100 = Stock.Create("XU100", "BIST 100 Endeksi", "Endeks");
        await db.Stocks.AddAsync(xu100);
        await db.SaveChangesAsync();
        logger.LogInformation("XU100 başarıyla eklendi.");
    }

    private static async Task SeedAdminAsync(AppDbContext db, ILogger logger, IPasswordHasher passwordHasher)
    {
        if (await db.Users.AnyAsync(u => u.Email == AdminEmail)) return;

        logger.LogInformation("Admin kullanıcısı oluşturuluyor...");
        var hash = passwordHasher.Hash(AdminPassword);
        var admin = User.Create(AdminEmail, hash, "Admin");
        await db.Users.AddAsync(admin);
        await db.SaveChangesAsync();
        logger.LogInformation("Admin kullanıcısı oluşturuldu: {Email}", AdminEmail);
    }
}
