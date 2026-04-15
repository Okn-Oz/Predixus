namespace Predixus.Application.Interfaces;

public record MlPredictionInput(string Symbol, int ForecastDays, List<StockPricePoint> HistoricalData);
public record StockPricePoint(DateOnly Date, decimal Open, decimal High, decimal Low, decimal Close, long Volume);
public record MlPredictionOutput(string Symbol, string ModelVersion, decimal Confidence, List<decimal> PredictedPrices);

public interface IMlPredictionClient
{
    Task<MlPredictionOutput> PredictAsync(MlPredictionInput input, CancellationToken ct = default);
}
