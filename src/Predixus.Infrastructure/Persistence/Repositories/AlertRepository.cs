using Microsoft.EntityFrameworkCore;
using Predixus.Domain.Entities;
using Predixus.Domain.Interfaces;
using Predixus.Infrastructure.Persistence;

namespace Predixus.Infrastructure.Persistence.Repositories;

public class AlertRepository(AppDbContext db) : IAlertRepository
{
    public async Task<List<Alert>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await db.Alerts
            .Where(a => a.UserId == userId && a.IsActive)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<Alert?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Alerts.FindAsync([id], ct);

    public async Task AddAsync(Alert alert, CancellationToken ct = default)
    {
        await db.Alerts.AddAsync(alert, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
