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

    private static readonly int[] AllowedForecastDays = [5, 10, 30];

    private Prediction() { }

    public static Prediction Create(Guid stockId, Guid userId, int forecastDays, decimal confidenceScore)
    {
        if (stockId == Guid.Empty) throw new ArgumentException("StockId geçersiz.");
        if (userId == Guid.Empty) throw new ArgumentException("UserId geçersiz.");
        if (!AllowedForecastDays.Contains(forecastDays))
            throw new ArgumentException("ForecastDays yalnızca 5, 10 veya 30 olabilir.");
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
