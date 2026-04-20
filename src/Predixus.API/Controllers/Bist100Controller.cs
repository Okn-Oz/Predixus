using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predixus.Application.DTOs;
using Predixus.Application.Interfaces;

namespace Predixus.API.Controllers;

[ApiController]
[Route("api/bist100")]
[Authorize]
public class Bist100Controller(IStockDataService stockDataService) : ControllerBase
{
    [HttpGet("prices")]
    public async Task<ActionResult<List<StockPriceDto>>> GetPrices(
        [FromQuery] int days = 60,
        CancellationToken ct = default)
    {
        var prices = await stockDataService.GetStockPricesAsync("XU100", days, ct);
        return Ok(prices);
    }

    [HttpPost("sync")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ForceSync(CancellationToken ct)
    {
        var count = await stockDataService.SyncStockPricesAsync("XU100", ct);
        return Ok(new { message = "Senkronizasyon tamamlandı.", newRecords = count });
    }
}
