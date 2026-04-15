using Predixus.Domain.Entities;

namespace Predixus.Domain.Interfaces;

public interface IPredictionRepository
{
    Task<Prediction?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Prediction>> GetBySymbolAsync(string symbol, int count, CancellationToken ct = default);
    Task AddAsync(Prediction prediction, CancellationToken ct = default);
}
