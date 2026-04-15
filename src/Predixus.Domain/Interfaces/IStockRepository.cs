using Predixus.Domain.Entities;

namespace Predixus.Domain.Interfaces;

public interface IStockRepository
{
    Task<Stock?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Stock?> GetBySymbolAsync(string symbol, CancellationToken ct = default);
    Task<List<Stock>> GetAllActiveAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(string symbol, CancellationToken ct = default);
    Task AddAsync(Stock stock, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
