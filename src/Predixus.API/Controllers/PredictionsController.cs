using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predixus.Application.DTOs;
using Predixus.Application.Interfaces;

namespace Predixus.API.Controllers;

[ApiController]
[Route("api/predictions")]
[Authorize]
public class PredictionsController(IPredictionService predictionService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PredictionResponseDto>> Predict(
        PredictionRequestDto request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var response = await predictionService.PredictAsync(userId, request, ct);

        if (response.FromCache)
            Response.Headers["X-Cache"] = "HIT";

        return Ok(response);
    }

    [HttpGet("{symbol}/history")]
    public async Task<ActionResult<List<PredictionResponseDto>>> GetHistory(
        string symbol,
        [FromQuery] int count = 10,
        CancellationToken ct = default)
    {
        var history = await predictionService.GetHistoryAsync(symbol, count, ct);
        return Ok(history);
    }

    [HttpGet("{predictionId:guid}/accuracy")]
    public async Task<ActionResult<AccuracyResponseDto>> GetAccuracy(
        Guid predictionId,
        CancellationToken ct)
    {
        var accuracy = await predictionService.GetAccuracyAsync(predictionId, ct);
        return Ok(accuracy);
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Kullanıcı kimliği bulunamadı.");

        return Guid.Parse(sub);
    }
}
