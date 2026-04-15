using Predixus.Application.DTOs;

namespace Predixus.Application.Interfaces;

public interface IYahooFinanceClient
{
    Task<List<StockPriceDto>> GetHistoricalDataAsync(string symbol, int days, CancellationToken ct = default);
}
