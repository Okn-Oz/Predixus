using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predixus.Application.DTOs;
using Predixus.Application.Interfaces;

namespace Predixus.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(IAdminService adminService) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsDto>> GetStats(CancellationToken ct)
    {
        var stats = await adminService.GetStatsAsync(ct);
        return Ok(stats);
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<UserSummaryDto>>> GetUsers(CancellationToken ct)
    {
        var users = await adminService.GetAllUsersAsync(ct);
        return Ok(users);
    }

    [HttpPut("users/{userId:guid}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid userId, CancellationToken ct)
    {
        await adminService.ToggleUserActiveAsync(userId, ct);
        return Ok(new { message = "Kullanıcı durumu güncellendi." });
    }

    [HttpPut("users/{userId:guid}/role")]
    public async Task<IActionResult> SetRole(Guid userId, SetRoleRequest request, CancellationToken ct)
    {
        await adminService.SetUserRoleAsync(userId, request.Role, ct);
        return Ok(new { message = "Rol güncellendi." });
    }
}
