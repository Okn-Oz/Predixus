namespace Predixus.Application.DTOs;

public record AlertDto(
    Guid Id,
    string Condition,
    decimal TargetPrice,
    bool IsActive,
    bool IsTriggered,
    DateTime CreatedAt
);

public record CreateAlertRequest(string Condition, decimal TargetPrice);
