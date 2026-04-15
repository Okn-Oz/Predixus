namespace Predixus.Domain.Entities;

public class Alert : BaseEntity
{
    private static readonly string[] AllowedConditions = ["ABOVE", "BELOW"];

    public Guid UserId { get; private set; }
    public Guid StockId { get; private set; }
    public Stock Stock { get; private set; } = null!;

    public string Condition { get; private set; } = string.Empty;
    public decimal TargetPrice { get; private set; }
    public bool IsTriggered { get; private set; }
    public bool IsActive { get; private set; }

    private Alert() { }

    public static Alert Create(Guid userId, Guid stockId, string condition, decimal targetPrice)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId geçersiz.");
        if (stockId == Guid.Empty) throw new ArgumentException("StockId geçersiz.");
        if (!AllowedConditions.Contains(condition?.ToUpperInvariant()))
            throw new ArgumentException("Condition yalnızca 'ABOVE' veya 'BELOW' olabilir.");
        if (targetPrice <= 0) throw new ArgumentException("TargetPrice sıfırdan büyük olmalıdır.");

        return new Alert
        {
            UserId = userId,
            StockId = stockId,
            Condition = condition!.ToUpperInvariant(),
            TargetPrice = targetPrice,
            IsTriggered = false,
            IsActive = true
        };
    }

    public void Trigger()
    {
        IsTriggered = true;
        SetUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdated();
    }
}
