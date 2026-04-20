using Predixus.Application.DTOs;
using Predixus.Application.Exceptions;
using Predixus.Application.Interfaces;
using Predixus.Domain.Entities;
using Predixus.Domain.Interfaces;

namespace Predixus.Application.Services;

public class AlertService(
    IAlertRepository alertRepository,
    IStockRepository stockRepository) : IAlertService
{
    public async Task<List<AlertDto>> GetUserAlertsAsync(Guid userId, CancellationToken ct = default)
    {
        var alerts = await alertRepository.GetByUserAsync(userId, ct);
        return alerts.Select(ToDto).ToList();
    }

    public async Task<AlertDto> CreateAlertAsync(Guid userId, CreateAlertRequest request, CancellationToken ct = default)
    {
        var stock = await stockRepository.GetBySymbolAsync("XU100", ct)
            ?? throw new NotFoundException("XU100 hissesi bulunamadı.");

        var alert = Alert.Create(userId, stock.Id, request.Condition, request.TargetPrice);
        await alertRepository.AddAsync(alert, ct);
        return ToDto(alert);
    }

    public async Task DeleteAlertAsync(Guid userId, Guid alertId, CancellationToken ct = default)
    {
        var alert = await alertRepository.GetByIdAsync(alertId, ct)
            ?? throw new NotFoundException("Alert bulunamadı.");

        if (alert.UserId != userId)
            throw new UnauthorizedException("Bu alert size ait değil.");

        alert.Deactivate();
        await alertRepository.SaveChangesAsync(ct);
    }

    private static AlertDto ToDto(Alert a) =>
        new(a.Id, a.Condition, a.TargetPrice, a.IsActive, a.IsTriggered, a.CreatedAt);
}
