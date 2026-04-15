using Predixus.Application.Interfaces;

namespace Predixus.API.BackgroundJobs;

public class StockDataFetchJob(
    IServiceScopeFactory scopeFactory,
    ILogger<StockDataFetchJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("StockDataFetchJob başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await SyncAllStocksAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task SyncAllStocksAsync(CancellationToken ct)
    {
        logger.LogInformation("Hisse fiyatları güncelleniyor...");

        using var scope = scopeFactory.CreateScope();
        var stockService = scope.ServiceProvider.GetRequiredService<IStockDataService>();

        try
        {
            var stocks = await stockService.GetAllActiveStocksAsync(ct);

            foreach (var stock in stocks)
            {
                try
                {
                    await stockService.SyncStockPricesAsync(stock.Symbol, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Symbol} fiyat güncellemesi başarısız.", stock.Symbol);
                }
            }

            logger.LogInformation("{Count} hisse güncellendi.", stocks.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Toplu hisse güncellemesi sırasında hata oluştu.");
        }
    }
}
