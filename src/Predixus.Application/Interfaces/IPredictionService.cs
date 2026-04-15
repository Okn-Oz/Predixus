using Predixus.Application.DTOs;

namespace Predixus.Application.Interfaces;

public interface IPredictionService
{
    Task<PredictionResponseDto> PredictAsync(Guid userId, PredictionRequestDto request, CancellationToken ct = default);
    Task<List<PredictionResponseDto>> GetHistoryAsync(string symbol, int count, CancellationToken ct = default);
    Task<AccuracyResponseDto> GetAccuracyAsync(Guid predictionId, CancellationToken ct = default);
}
