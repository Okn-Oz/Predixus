namespace Predixus.Application.Interfaces;

// ML servisine gönderilecek veri (CSV'ye dönüştürülür)
public record StockPricePoint(DateOnly Date, decimal Open, decimal High, decimal Low, decimal Close, long Volume);

// ML servisine giden istek: sadece geçmiş OHLCV verisi (min 24 gün)
public record MlPredictionInput(List<StockPricePoint> HistoricalData);

// ML servisinden gelen yanıt: ertesi günün tahmini açılış fiyatı
public record MlPredictionOutput(decimal PredictedPrice);

public interface IMlPredictionClient
{
    Task<MlPredictionOutput> PredictAsync(MlPredictionInput input, CancellationToken ct = default);
}
