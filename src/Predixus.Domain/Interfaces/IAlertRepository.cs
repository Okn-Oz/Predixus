using Predixus.Domain.Entities;

namespace Predixus.Domain.Interfaces;

public interface IAlertRepository
{
    Task<List<Alert>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<Alert?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Alert alert, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
