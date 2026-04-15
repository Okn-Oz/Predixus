using Microsoft.EntityFrameworkCore;
using Predixus.Domain.Entities;
using Predixus.Domain.Interfaces;

namespace Predixus.Infrastructure.Persistence.Repositories;

public class StockPriceRepository(AppDbContext db) : IStockPriceRepository
{
    public async Task<List<StockPrice>> GetBySymbolAsync(string symbol, int days, CancellationToken ct = default)
        => await db.StockPrices
            .Where(sp => sp.Stock.Symbol == symbol.ToUpperInvariant())
            .OrderByDescending(sp => sp.Date)
            .Take(days)
            .ToListAsync(ct);

    public async Task<StockPrice?> GetLatestAsync(string symbol, CancellationToken ct = default)
        => await db.StockPrices
            .Where(sp => sp.Stock.Symbol == symbol.ToUpperInvariant())
            .OrderByDescending(sp => sp.Date)
            .FirstOrDefaultAsync(ct);

    public async Task<bool> ExistsAsync(string symbol, DateOnly date, CancellationToken ct = default)
        => await db.StockPrices
            .AnyAsync(sp => sp.Stock.Symbol == symbol.ToUpperInvariant() && sp.Date == date, ct);

    public async Task AddRangeAsync(IEnumerable<StockPrice> prices, CancellationToken ct = default)
    {
        await db.StockPrices.AddRangeAsync(prices, ct);
        await db.SaveChangesAsync(ct);
    }
}
