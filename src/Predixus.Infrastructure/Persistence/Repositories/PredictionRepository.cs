using Microsoft.EntityFrameworkCore;
using Predixus.Domain.Entities;
using Predixus.Domain.Interfaces;

namespace Predixus.Infrastructure.Persistence.Repositories;

public class PredictionRepository(AppDbContext db) : IPredictionRepository
{
    public async Task<Prediction?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Predictions
            .Include(p => p.Points)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<List<Prediction>> GetBySymbolAsync(string symbol, int count, CancellationToken ct = default)
        => await db.Predictions
            .Include(p => p.Points)
            .Where(p => p.Stock.Symbol == symbol.ToUpperInvariant())
            .OrderByDescending(p => p.PredictedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task AddAsync(Prediction prediction, CancellationToken ct = default)
    {
        await db.Predictions.AddAsync(prediction, ct);
        await db.SaveChangesAsync(ct);
    }
}
