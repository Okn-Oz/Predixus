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
    private const string Bist100Symbol = "XU100";
    private const string CacheKey = "prediction:bist100:1d";
    private const int MinRequiredDays = 24;
    private const int HistoryDays = 60;

    public async Task<PredictionResponseDto> PredictAsync(Guid userId, CancellationToken ct = default)
    {
        // 1. Redis cache kontrolü
        var cached = await cache.GetAsync<PredictionResponseDto>(CacheKey, ct);
        if (cached is not null)
        {
            logger.LogInformation("Cache HIT: {CacheKey}", CacheKey);
            return cached with { FromCache = true };
        }

        // 2. BIST100 verisi DB'de var mı?
        var stock = await stockRepository.GetBySymbolAsync(Bist100Symbol, ct)
            ?? throw new NotFoundException("BIST100 (XU100) verisi bulunamadı.");

        // 3. Geçmiş fiyat verisi (min 24 gün)
        var prices = await stockPriceRepository.GetBySymbolAsync(Bist100Symbol, days: HistoryDays, ct);
        if (prices.Count < MinRequiredDays)
            throw new InsufficientDataException(
                $"Yeterli fiyat verisi yok. Minimum {MinRequiredDays} gün gerekli, mevcut: {prices.Count}.");

        // 4. ML servisine gönder
        var mlInput = new MlPredictionInput(
            HistoricalData: prices
                .OrderBy(p => p.Date)
                .Select(p => new StockPricePoint(p.Date, p.Open, p.High, p.Low, p.Close, p.Volume))
                .ToList()
        );

        var mlOutput = await mlClient.PredictAsync(mlInput, ct);

        // 5. DB'ye kaydet
        var prediction = Prediction.Create(stock.Id, userId, forecastDays: 1, confidenceScore: 0m);
        prediction.Points.Add(PredictionPoint.Create(prediction.Id, dayOffset: 1, mlOutput.PredictedPrice));
        await predictionRepository.AddAsync(prediction, ct);

        // 6. Response oluştur
        var response = new PredictionResponseDto(
            PredictionId: prediction.Id,
            PredictedPrice: mlOutput.PredictedPrice,
            PredictedAt: prediction.PredictedAt,
            FromCache: false
        );

        // 7. Cache'le
        await cache.SetAsync(CacheKey, response, TimeUntilEndOfDay(), ct);

        logger.LogInformation("BIST100 tahmini oluşturuldu: {PredictedPrice}", mlOutput.PredictedPrice);

        return response;
    }

    public async Task<List<PredictionResponseDto>> GetHistoryAsync(Guid userId, int count, CancellationToken ct = default)
    {
        var predictions = await predictionRepository.GetByUserAsync(userId, Bist100Symbol, count, ct);
        return predictions.Select(p => new PredictionResponseDto(
            PredictionId: p.Id,
            PredictedPrice: p.Points.FirstOrDefault()?.PredictedPrice ?? 0,
            PredictedAt: p.PredictedAt,
            FromCache: false
        )).ToList();
    }

    public async Task<AccuracyResponseDto> GetAccuracyAsync(Guid predictionId, CancellationToken ct = default)
    {
        var prediction = await predictionRepository.GetByIdAsync(predictionId, ct)
            ?? throw new NotFoundException($"Tahmin bulunamadı: {predictionId}");

        var actualized = prediction.Points.Where(p => p.ActualPrice.HasValue).ToList();

        if (actualized.Count == 0)
            return new AccuracyResponseDto(predictionId, prediction.Points.Count, 0, null, null);

        var mae = actualized.Average(p => Math.Abs((double)(p.ActualPrice!.Value - p.PredictedPrice)));
        var mape = actualized.Average(p =>
            p.PredictedPrice == 0 ? 0 :
            Math.Abs((double)((p.ActualPrice!.Value - p.PredictedPrice) / p.PredictedPrice)) * 100);

        return new AccuracyResponseDto(
            predictionId,
            prediction.Points.Count, actualized.Count,
            (decimal)Math.Round(mae, 4),
            (decimal)Math.Round(mape, 2)
        );
    }

    private static TimeSpan TimeUntilEndOfDay()
    {
        var now = DateTime.UtcNow;
        var ttl = now.Date.AddDays(1) - now;
        return ttl > TimeSpan.Zero ? ttl : TimeSpan.FromHours(1);
    }
}
