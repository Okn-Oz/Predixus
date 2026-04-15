using Predixus.Application.DTOs;

namespace Predixus.Application.Interfaces;

public interface IStockDataService
{
    Task<List<StockDto>> GetAllActiveStocksAsync(CancellationToken ct = default);
    Task<StockDto?> GetStockAsync(string symbol, CancellationToken ct = default);
    Task<List<StockPriceDto>> GetStockPricesAsync(string symbol, int days, CancellationToken ct = default);
    Task SyncStockPricesAsync(string symbol, CancellationToken ct = default);
}
