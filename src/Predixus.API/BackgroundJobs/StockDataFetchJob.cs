using Predixus.Application.Interfaces;
using Predixus.Domain.Interfaces;

namespace Predixus.API.BackgroundJobs;

public class StockDataFetchJob(
    IServiceScopeFactory scopeFactory,
    ILogger<StockDataFetchJob> logger) : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(12);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("StockDataFetchJob başlatıldı. {Delay} saniye sonra ilk sync yapılacak.", InitialDelay.TotalSeconds);

        await Task.Delay(InitialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await SyncAllStocksAsync(stoppingToken);
            await FillActualPricesAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task SyncAllStocksAsync(CancellationToken ct)
    {
        logger.LogInformation("XU100 fiyatları güncelleniyor...");

        using var scope = scopeFactory.CreateScope();
        var stockService = scope.ServiceProvider.GetRequiredService<IStockDataService>();

        try
        {
            var stocks = await stockService.GetAllActiveStocksAsync(ct);

            foreach (var stock in stocks)
            {
                try
                {
                    var count = await stockService.SyncStockPricesAsync(stock.Symbol, ct);
                    if (count > 0)
                        logger.LogInformation("{Symbol}: {Count} yeni fiyat kaydı eklendi.", stock.Symbol, count);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Symbol} fiyat güncellemesi başarısız.", stock.Symbol);
                }
            }

            logger.LogInformation("{Count} hisse kontrol edildi.", stocks.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fiyat güncellemesi sırasında hata oluştu.");
        }
    }

    private async Task FillActualPricesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var predictionRepo = scope.ServiceProvider.GetRequiredService<IPredictionRepository>();
        var priceRepo = scope.ServiceProvider.GetRequiredService<IStockPriceRepository>();

        try
        {
            var pending = await predictionRepo.GetPendingActualPricePointsAsync(ct);
            if (pending.Count == 0) return;

            int filled = 0;
            foreach (var point in pending)
            {
                // Tahmin gününün +DayOffset sonraki işlem günü
                var targetDate = DateOnly.FromDateTime(point.Prediction.PredictedAt.Date)
                    .AddDays(point.DayOffset);

                // Hafta sonu varsa sonraki Pazartesi'ye kadar ilerle
                while (targetDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    targetDate = targetDate.AddDays(1);

                var price = await priceRepo.GetByDateAsync("XU100", targetDate, ct);
                if (price is null) continue;

                point.SetActualPrice(price.Open);
                filled++;
            }

            if (filled > 0)
            {
                await predictionRepo.SaveChangesAsync(ct);
                logger.LogInformation("{Count} tahmin noktası için gerçek fiyat güncellendi.", filled);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gerçek fiyat doldurma sırasında hata oluştu.");
        }
    }
}
