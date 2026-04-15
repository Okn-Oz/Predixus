namespace Predixus.Application.DTOs;

public record StockDto(
    Guid Id,
    string Symbol,
    string Name,
    string? Sector,
    bool IsActive
);

public record StockPriceDto(
    DateOnly Date,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume,
    decimal DailyChangePercent
);
