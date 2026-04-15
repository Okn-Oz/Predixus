namespace Predixus.Domain.Entities;

public class Stock : BaseEntity
{
    public string Symbol { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Sector { get; private set; }
    public bool IsActive { get; private set; }


    //navigation property -> 1:M  stock-StockPrices relation with FK
    public ICollection<StockPrice> Prices { get; private set; } = new List<StockPrice>();
    public ICollection<Prediction> Predictions { get; private set; } = new List<Prediction>();

    // Factory Method Design Pattern Aplied
    private Stock() { }

    public static Stock Create(string symbol, string name, string? sector = null)
    {
        if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Symbol boş olamaz.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name boş olamaz.");

        return new Stock
        {
            Symbol = symbol.ToUpperInvariant(),
            Name = name,
            Sector = sector,
            IsActive = true
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
