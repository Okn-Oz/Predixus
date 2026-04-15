using Microsoft.EntityFrameworkCore;
using Predixus.Domain.Entities;
using Predixus.Domain.Interfaces;

namespace Predixus.Infrastructure.Persistence.Repositories;

public class StockRepository(AppDbContext db) : IStockRepository
{
    public async Task<Stock?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Stocks.FindAsync([id], ct);

    public async Task<Stock?> GetBySymbolAsync(string symbol, CancellationToken ct = default)
        => await db.Stocks.FirstOrDefaultAsync(s => s.Symbol == symbol.ToUpperInvariant(), ct);

    public async Task<List<Stock>> GetAllActiveAsync(CancellationToken ct = default)
        => await db.Stocks.Where(s => s.IsActive).OrderBy(s => s.Symbol).ToListAsync(ct);

    public async Task<bool> ExistsAsync(string symbol, CancellationToken ct = default)
        => await db.Stocks.AnyAsync(s => s.Symbol == symbol.ToUpperInvariant(), ct);

    public async Task AddAsync(Stock stock, CancellationToken ct = default)
        => await db.Stocks.AddAsync(stock, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
