namespace Predixus.Domain.Entities;

public class Prediction : BaseEntity
{
    public Guid StockId { get; private set; }
    public Guid UserId { get; private set; }
    public Stock Stock { get; private set; } = null!;
    public DateTime PredictedAt { get; private set; }
    public int ForecastDays { get; private set; }
    public decimal ConfidenceScore { get; private set; }

    public ICollection<PredictionPoint> Points { get; private set; } = new List<PredictionPoint>();

    // Şu an sadece 1 (ertesi gün) destekleniyor.
    // Arkadaşın 5, 10, 30 günlük modelleri eklediğinde buraya eklenecek.
    private static readonly int[] SupportedForecastDays = [1];

    private Prediction() { }

    public static Prediction Create(Guid stockId, Guid userId, int forecastDays, decimal confidenceScore)
    {
        if (stockId == Guid.Empty) throw new ArgumentException("StockId geçersiz.");
        if (userId == Guid.Empty) throw new ArgumentException("UserId geçersiz.");
        if (!SupportedForecastDays.Contains(forecastDays))
            throw new ArgumentException($"ForecastDays şu an yalnızca {string.Join(", ", SupportedForecastDays)} olabilir.");
        if (confidenceScore < 0 || confidenceScore > 1)
            throw new ArgumentException("ConfidenceScore 0 ile 1 arasında olmalıdır.");

        return new Prediction
        {
            StockId = stockId,
            UserId = userId,
            ForecastDays = forecastDays,
            ConfidenceScore = confidenceScore,
            PredictedAt = DateTime.UtcNow
        };
    }
}
