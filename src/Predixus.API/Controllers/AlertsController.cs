using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predixus.Application.DTOs;
using Predixus.Application.Interfaces;

namespace Predixus.API.Controllers;

[ApiController]
[Route("api/alerts")]
[Authorize]
public class AlertsController(IAlertService alertService) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<List<AlertDto>>> GetAlerts(CancellationToken ct)
    {
        var alerts = await alertService.GetUserAlertsAsync(UserId, ct);
        return Ok(alerts);
    }

    [HttpPost]
    public async Task<ActionResult<AlertDto>> CreateAlert(CreateAlertRequest request, CancellationToken ct)
    {
        var alert = await alertService.CreateAlertAsync(UserId, request, ct);
        return Created($"/api/alerts/{alert.Id}", alert);
    }

    [HttpDelete("{alertId:guid}")]
    public async Task<IActionResult> DeleteAlert(Guid alertId, CancellationToken ct)
    {
        await alertService.DeleteAlertAsync(UserId, alertId, ct);
        return NoContent();
    }
}
