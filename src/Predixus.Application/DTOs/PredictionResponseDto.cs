namespace Predixus.Application.DTOs;

public record PredictionResponseDto(
    Guid PredictionId,
    string Symbol,
    int ForecastDays,
    decimal Confidence,
    DateTime PredictedAt,
    bool FromCache,
    List<PredictionPointDto> Points
);

public record PredictionPointDto(
    int DayOffset,
    decimal PredictedPrice,
    decimal? ActualPrice
);

public record AccuracyResponseDto(
    Guid PredictionId,
    string Symbol,
    int TotalPoints,
    int ActualizedPoints,
    decimal? MeanAbsoluteError,
    decimal? MeanAbsolutePercentageError
);
