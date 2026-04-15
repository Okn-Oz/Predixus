using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predixus.Application.DTOs;
using Predixus.Application.Interfaces;

namespace Predixus.API.Controllers;

[ApiController]
[Route("api/stocks")]
[Authorize]
public class StocksController(IStockDataService stockService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<StockDto>>> GetAll(CancellationToken ct)
    {
        var stocks = await stockService.GetAllActiveStocksAsync(ct);
        return Ok(stocks);
    }

    [HttpGet("{symbol}")]
    public async Task<ActionResult<StockDto>> GetBySymbol(string symbol, CancellationToken ct)
    {
        var stock = await stockService.GetStockAsync(symbol, ct);
        if (stock is null) return NotFound(new { message = $"'{symbol}' sembolü bulunamadı." });
        return Ok(stock);
    }

    [HttpGet("{symbol}/prices")]
    public async Task<ActionResult<List<StockPriceDto>>> GetPrices(
        string symbol,
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var prices = await stockService.GetStockPricesAsync(symbol, days, ct);
        return Ok(prices);
    }
}
