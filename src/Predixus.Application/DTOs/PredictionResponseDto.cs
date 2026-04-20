namespace Predixus.Application.DTOs;

public record PredictionResponseDto(
    Guid PredictionId,
    decimal PredictedPrice,
    DateTime PredictedAt,
    bool FromCache
);

public record AccuracyResponseDto(
    Guid PredictionId,
    int TotalPoints,
    int ActualizedPoints,
    decimal? MeanAbsoluteError,
    decimal? MeanAbsolutePercentageError
);
