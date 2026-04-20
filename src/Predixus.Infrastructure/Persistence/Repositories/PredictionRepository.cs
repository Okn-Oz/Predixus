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

    public async Task<List<Prediction>> GetByUserAsync(Guid userId, string symbol, int count, CancellationToken ct = default)
        => await db.Predictions
            .Include(p => p.Points)
            .Where(p => p.UserId == userId && p.Stock.Symbol == symbol.ToUpperInvariant())
            .OrderByDescending(p => p.PredictedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task AddAsync(Prediction prediction, CancellationToken ct = default)
    {
        await db.Predictions.AddAsync(prediction, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> CountTotalAsync(CancellationToken ct = default)
        => await db.Predictions.CountAsync(ct);

    public async Task<List<PredictionPoint>> GetPendingActualPricePointsAsync(CancellationToken ct = default)
        => await db.PredictionPoints
            .Where(pp => pp.ActualPrice == null &&
                         pp.Prediction.PredictedAt < DateTime.UtcNow.Date)
            .Include(pp => pp.Prediction)
            .ToListAsync(ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
