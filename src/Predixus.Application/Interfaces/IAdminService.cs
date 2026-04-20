using Predixus.Application.DTOs;

namespace Predixus.Application.Interfaces;

public interface IAdminService
{
    Task<List<UserSummaryDto>> GetAllUsersAsync(CancellationToken ct = default);
    Task<AdminStatsDto> GetStatsAsync(CancellationToken ct = default);
    Task ToggleUserActiveAsync(Guid userId, CancellationToken ct = default);
    Task SetUserRoleAsync(Guid userId, string role, CancellationToken ct = default);
}
