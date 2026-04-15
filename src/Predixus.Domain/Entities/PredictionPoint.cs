namespace Predixus.Domain.Entities;

public class PredictionPoint : BaseEntity
{
    public Guid PredictionId { get; private set; }
    public int DayOffset { get; private set; }
    public decimal PredictedPrice { get; private set; }
    public decimal? ActualPrice { get; private set; }

    private PredictionPoint() { }

    public static PredictionPoint Create(Guid predictionId, int dayOffset, decimal predictedPrice)
    {
        if (predictionId == Guid.Empty) throw new ArgumentException("PredictionId geçersiz.");
        if (dayOffset < 1) throw new ArgumentException("DayOffset 1'den küçük olamaz.");
        if (predictedPrice <= 0) throw new ArgumentException("PredictedPrice sıfırdan büyük olmalıdır.");

        return new PredictionPoint
        {
            PredictionId = predictionId,
            DayOffset = dayOffset,
            PredictedPrice = predictedPrice
        };
    }

    public void SetActualPrice(decimal actual)
    {
        if (actual <= 0) throw new ArgumentException("ActualPrice sıfırdan büyük olmalıdır.");
        ActualPrice = actual;
        SetUpdated();
    }
}
