namespace Predixus.Application.DTOs;

public record UserSummaryDto(
    Guid Id,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    int PredictionCount
);

public record AdminStatsDto(
    int TotalUsers,
    int ActiveUsers,
    int TotalPredictions
);

public record SetRoleRequest(string Role);
