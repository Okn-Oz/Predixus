using Predixus.Domain.Entities;

namespace Predixus.Domain.Interfaces;

public interface IStockPriceRepository
{
    Task<List<StockPrice>> GetBySymbolAsync(string symbol, int days, CancellationToken ct = default);
    Task<StockPrice?> GetLatestAsync(string symbol, CancellationToken ct = default);
    Task<bool> ExistsAsync(string symbol, DateOnly date, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<StockPrice> prices, CancellationToken ct = default);
    Task<StockPrice?> GetByDateAsync(string symbol, DateOnly date, CancellationToken ct = default);
}
