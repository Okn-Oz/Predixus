using Microsoft.Extensions.Logging;
using Predixus.Application.DTOs;
using Predixus.Application.Exceptions;
using Predixus.Application.Interfaces;
using Predixus.Domain.Entities;
using Predixus.Domain.Interfaces;

namespace Predixus.Application.Services;

public class StockDataService(
    IStockRepository stockRepository,
    IStockPriceRepository stockPriceRepository,
    IYahooFinanceClient yahooClient,
    ILogger<StockDataService> logger) : IStockDataService
{
    public async Task<List<StockDto>> GetAllActiveStocksAsync(CancellationToken ct = default)
    {
        var stocks = await stockRepository.GetAllActiveAsync(ct);
        return stocks.Select(s => new StockDto(s.Id, s.Symbol, s.Name, s.Sector, s.IsActive)).ToList();
    }

    public async Task<StockDto?> GetStockAsync(string symbol, CancellationToken ct = default)
    {
        var stock = await stockRepository.GetBySymbolAsync(symbol, ct);
        return stock is null ? null : new StockDto(stock.Id, stock.Symbol, stock.Name, stock.Sector, stock.IsActive);
    }

    public async Task<List<StockPriceDto>> GetStockPricesAsync(string symbol, int days, CancellationToken ct = default)
    {
        _ = await stockRepository.GetBySymbolAsync(symbol, ct)
            ?? throw new NotFoundException($"'{symbol}' sembolü bulunamadı.");

        var prices = await stockPriceRepository.GetBySymbolAsync(symbol, days, ct);
        return prices.Select(sp => new StockPriceDto(
            sp.Date, sp.Open, sp.High, sp.Low, sp.Close, sp.Volume, sp.DailyChangePercent)).ToList();
    }

    public async Task SyncStockPricesAsync(string symbol, CancellationToken ct = default)
    {
        var stock = await stockRepository.GetBySymbolAsync(symbol, ct)
            ?? throw new NotFoundException($"'{symbol}' sembolü DB'de bulunamadı.");

        logger.LogInformation("Fiyat senkronizasyonu başlıyor: {Symbol}", symbol);

        var yahooData = await yahooClient.GetHistoricalDataAsync(symbol, days: 60, ct);

        var newPrices = new List<StockPrice>();

        foreach (var point in yahooData)
        {
            if (await stockPriceRepository.ExistsAsync(symbol, point.Date, ct))
                continue;

            newPrices.Add(StockPrice.Create(
                stockId: stock.Id,
                date: point.Date,
                open: point.Open,
                high: point.High,
                low: point.Low,
                close: point.Close,
                volume: point.Volume
            ));
        }

        if (newPrices.Count == 0)
        {
            logger.LogInformation("{Symbol} için yeni fiyat verisi yok.", symbol);
            return;
        }

        await stockPriceRepository.AddRangeAsync(newPrices, ct);
        logger.LogInformation("{Symbol} için {Count} yeni fiyat kaydedildi.", symbol, newPrices.Count);
    }
}
