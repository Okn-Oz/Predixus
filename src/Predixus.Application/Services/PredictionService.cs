using Microsoft.Extensions.Logging;
using Predixus.Application.DTOs;
using Predixus.Application.Exceptions;
using Predixus.Application.Interfaces;
using Predixus.Domain.Entities;
using Predixus.Domain.Interfaces;

namespace Predixus.Application.Services;

public class PredictionService(
    IStockRepository stockRepository,
    IStockPriceRepository stockPriceRepository,
    IPredictionRepository predictionRepository,
    ICacheService cache,
    IMlPredictionClient mlClient,
    ILogger<PredictionService> logger) : IPredictionService
{
    public async Task<PredictionResponseDto> PredictAsync(
        Guid userId,
        PredictionRequestDto request,
        CancellationToken ct = default)
    {
        var symbol = request.Symbol.ToUpperInvariant();
        var cacheKey = $"prediction:{symbol}:{request.ForecastDays}d";

        // 1. Redis cache kontrolü
        var cached = await cache.GetAsync<PredictionResponseDto>(cacheKey, ct);
        if (cached is not null)
        {
            logger.LogInformation("Cache HIT: {CacheKey}", cacheKey);
            return cached with { FromCache = true };
        }

        // 2. Hisse DB'de var mı?
        var stock = await stockRepository.GetBySymbolAsync(symbol, ct)
            ?? throw new NotFoundException($"'{symbol}' sembolü bulunamadı.");

        // 3. Son 60 günlük fiyat (min 30 gün şartı)
        var prices = await stockPriceRepository.GetBySymbolAsync(symbol, days: 60, ct);
        if (prices.Count < 30)
            throw new InsufficientDataException(
                $"'{symbol}' için yeterli fiyat verisi yok. Minimum 30 gün gerekli, mevcut: {prices.Count}.");

        // 4. ML servisine gönder
        var mlInput = new MlPredictionInput(
            Symbol: symbol,
            ForecastDays: request.ForecastDays,
            HistoricalData: prices
                .OrderBy(p => p.Date)
                .Select(p => new StockPricePoint(p.Date, p.Open, p.High, p.Low, p.Close, p.Volume))
                .ToList()
        );

        var mlOutput = await mlClient.PredictAsync(mlInput, ct);

        // 5. Prediction entity oluştur ve DB'ye kaydet
        var prediction = Prediction.Create(stock.Id, userId, request.ForecastDays, mlOutput.Confidence);

        for (int i = 0; i < mlOutput.PredictedPrices.Count; i++)
        {
            prediction.Points.Add(PredictionPoint.Create(
                predictionId: prediction.Id,
                dayOffset: i + 1,
                predictedPrice: mlOutput.PredictedPrices[i]
            ));
        }

        await predictionRepository.AddAsync(prediction, ct);

        // 6. Response DTO oluştur
        var response = ToDto(prediction, symbol, fromCache: false);

        // 7. Redis'e cache'le (TTL = gün sonu)
        await cache.SetAsync(cacheKey, response, TimeUntilEndOfDay(), ct);

        logger.LogInformation(
            "Tahmin oluşturuldu: {Symbol}, {Days} gün, confidence={Confidence}",
            symbol, request.ForecastDays, mlOutput.Confidence);

        return response;
    }

    public async Task<List<PredictionResponseDto>> GetHistoryAsync(
        string symbol, int count, CancellationToken ct = default)
    {
        var predictions = await predictionRepository.GetBySymbolAsync(symbol, count, ct);
        return predictions.Select(p => ToDto(p, symbol.ToUpperInvariant(), fromCache: false)).ToList();
    }

    public async Task<AccuracyResponseDto> GetAccuracyAsync(Guid predictionId, CancellationToken ct = default)
    {
        var prediction = await predictionRepository.GetByIdAsync(predictionId, ct)
            ?? throw new NotFoundException($"Tahmin bulunamadı: {predictionId}");

        var stock = await stockRepository.GetByIdAsync(prediction.StockId, ct);
        var symbol = stock?.Symbol ?? "UNKNOWN";

        var actualized = prediction.Points.Where(p => p.ActualPrice.HasValue).ToList();

        if (actualized.Count == 0)
            return new AccuracyResponseDto(predictionId, symbol, prediction.Points.Count, 0, null, null);

        var mae = actualized.Average(p => Math.Abs((double)(p.ActualPrice!.Value - p.PredictedPrice)));
        var mape = actualized.Average(p =>
            p.PredictedPrice == 0 ? 0 :
            Math.Abs((double)((p.ActualPrice!.Value - p.PredictedPrice) / p.PredictedPrice)) * 100);

        return new AccuracyResponseDto(
            predictionId, symbol,
            prediction.Points.Count, actualized.Count,
            (decimal)Math.Round(mae, 4),
            (decimal)Math.Round(mape, 2)
        );
    }

    private static PredictionResponseDto ToDto(Prediction p, string symbol, bool fromCache) => new(
        PredictionId: p.Id,
        Symbol: symbol,
        ForecastDays: p.ForecastDays,
        Confidence: p.ConfidenceScore,
        PredictedAt: p.PredictedAt,
        FromCache: fromCache,
        Points: p.Points
            .OrderBy(pp => pp.DayOffset)
            .Select(pp => new PredictionPointDto(pp.DayOffset, pp.PredictedPrice, pp.ActualPrice))
            .ToList()
    );

    private static TimeSpan TimeUntilEndOfDay()
    {
        var now = DateTime.UtcNow;
        var endOfDay = now.Date.AddDays(1);
        var ttl = endOfDay - now;
        return ttl > TimeSpan.Zero ? ttl : TimeSpan.FromHours(1);
    }
}
