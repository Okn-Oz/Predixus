using Predixus.Application.DTOs;

namespace Predixus.Application.Interfaces;

public interface IAlertService
{
    Task<List<AlertDto>> GetUserAlertsAsync(Guid userId, CancellationToken ct = default);
    Task<AlertDto> CreateAlertAsync(Guid userId, CreateAlertRequest request, CancellationToken ct = default);
    Task DeleteAlertAsync(Guid userId, Guid alertId, CancellationToken ct = default);
}
