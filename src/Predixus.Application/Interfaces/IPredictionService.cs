using Predixus.Application.DTOs;

namespace Predixus.Application.Interfaces;

public interface IPredictionService
{
    Task<PredictionResponseDto> PredictAsync(Guid userId, CancellationToken ct = default);
    Task<List<PredictionResponseDto>> GetHistoryAsync(Guid userId, int count, CancellationToken ct = default);
    Task<AccuracyResponseDto> GetAccuracyAsync(Guid predictionId, CancellationToken ct = default);
}
