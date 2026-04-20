using Predixus.Application.DTOs;
using Predixus.Application.Exceptions;
using Predixus.Application.Interfaces;
using Predixus.Domain.Interfaces;

namespace Predixus.Application.Services;

public class AdminService(
    IUserRepository userRepository,
    IPredictionRepository predictionRepository) : IAdminService
{
    public async Task<List<UserSummaryDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = await userRepository.GetAllAsync(ct);
        return users.Select(u => new UserSummaryDto(
            u.Id,
            u.Email,
            u.Role,
            u.IsActive,
            u.CreatedAt,
            u.Predictions.Count
        )).ToList();
    }

    public async Task<AdminStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var users = await userRepository.GetAllAsync(ct);
        var totalPredictions = await predictionRepository.CountTotalAsync(ct);

        return new AdminStatsDto(
            TotalUsers: users.Count,
            ActiveUsers: users.Count(u => u.IsActive),
            TotalPredictions: totalPredictions
        );
    }

    public async Task ToggleUserActiveAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException($"Kullanıcı bulunamadı: {userId}");

        if (user.IsActive)
            user.Deactivate();
        else
            user.Activate();

        await userRepository.SaveChangesAsync(ct);
    }

    public async Task SetUserRoleAsync(Guid userId, string role, CancellationToken ct = default)
    {
        if (role != "User" && role != "Admin")
            throw new ArgumentException("Geçersiz rol. 'User' veya 'Admin' olmalı.");

        var user = await userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException($"Kullanıcı bulunamadı: {userId}");

        user.SetRole(role);
        await userRepository.SaveChangesAsync(ct);
    }
}
