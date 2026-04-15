namespace Predixus.Domain.Entities;

public class StockPrice : BaseEntity
{
    public Guid StockId { get; private set; }
    public Stock Stock { get; private set; } = null!;

    public DateOnly Date { get; private set; }
    public decimal Open { get; private set; }
    public decimal High { get; private set; }
    public decimal Low { get; private set; }
    public decimal Close { get; private set; }
    public long Volume { get; private set; }

    public decimal DailyChangePercent =>
        Open == 0 ? 0 : Math.Round((Close - Open) / Open * 100, 2);

    private StockPrice() { }

    public static StockPrice Create(
        Guid stockId,
        DateOnly date,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        long volume)
    {
        if (stockId == Guid.Empty) throw new ArgumentException("StockId geçersiz.");
        if (open <= 0 || high <= 0 || low <= 0 || close <= 0)
            throw new ArgumentException("Fiyat değerleri sıfırdan büyük olmalıdır.");
        if (high < low)
            throw new ArgumentException("High, Low'dan küçük olamaz.");
        if (volume < 0)
            throw new ArgumentException("Volume negatif olamaz.");

        return new StockPrice
        {
            StockId = stockId,
            Date = date,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = volume
        };
    }
}
