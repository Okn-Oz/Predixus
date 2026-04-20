using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Predixus.Application.DTOs;
using Predixus.Application.Exceptions;
using Predixus.Application.Interfaces;

namespace Predixus.Infrastructure.ExternalServices;

public class YahooFinanceClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<YahooFinanceClient> logger) : IYahooFinanceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly int _requestDelayMs = int.TryParse(configuration["YahooFinance:RequestDelayMs"], out var delay) ? delay : 1000;

    public async Task<List<StockPriceDto>> GetHistoricalDataAsync(
        string symbol,
        int days,
        CancellationToken ct = default)
    {
        var yahooSymbol = symbol.ToUpperInvariant().EndsWith(".IS")
            ? symbol.ToUpperInvariant()
            : $"{symbol.ToUpperInvariant()}.IS";

        var period2 = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var period1 = DateTimeOffset.UtcNow.AddDays(-days * 1.5).ToUnixTimeSeconds();

        var url = $"/v8/finance/chart/{yahooSymbol}?period1={period1}&period2={period2}&interval=1d";

        logger.LogInformation("Yahoo Finance'tan veri çekiliyor: {Symbol}, {Days} gün", yahooSymbol, days);

        try
        {
            await Task.Delay(_requestDelayMs, ct);

            // Yahoo Finance browser gibi header bekliyor
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            request.Headers.Add("Accept", "application/json,text/html,*/*");
            request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
            request.Headers.Add("Referer", "https://finance.yahoo.com/");

            var response = await httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            return ParseResponse(json, days);
        }
        catch (ExternalServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Yahoo Finance hatası: {Symbol}", yahooSymbol);
            throw new ExternalServiceException("YahooFinance", ex.Message);
        }
    }

    private static List<StockPriceDto> ParseResponse(string json, int requestedDays)
    {
        using var doc = JsonDocument.Parse(json);
        var result = doc.RootElement
            .GetProperty("chart")
            .GetProperty("result")[0];

        var timestamps = result.GetProperty("timestamp").EnumerateArray().ToList();
        var quote = result.GetProperty("indicators").GetProperty("quote")[0];

        var opens   = quote.GetProperty("open").EnumerateArray().ToList();
        var highs   = quote.GetProperty("high").EnumerateArray().ToList();
        var lows    = quote.GetProperty("low").EnumerateArray().ToList();
        var closes  = quote.GetProperty("close").EnumerateArray().ToList();
        var volumes = quote.GetProperty("volume").EnumerateArray().ToList();

        var points = new List<StockPriceDto>();

        for (int i = 0; i < timestamps.Count; i++)
        {
            if (opens[i].ValueKind == JsonValueKind.Null || closes[i].ValueKind == JsonValueKind.Null)
                continue;

            var date = DateTimeOffset.FromUnixTimeSeconds(timestamps[i].GetInt64()).UtcDateTime;
            var open  = opens[i].GetDecimal();
            var close = closes[i].GetDecimal();
            var dailyChange = open == 0 ? 0 : Math.Round((close - open) / open * 100, 2);

            points.Add(new StockPriceDto(
                Date: DateOnly.FromDateTime(date),
                Open: open,
                High: highs[i].GetDecimal(),
                Low: lows[i].GetDecimal(),
                Close: close,
                Volume: volumes[i].ValueKind != JsonValueKind.Null ? volumes[i].GetInt64() : 0,
                DailyChangePercent: dailyChange
            ));
        }

        return points.OrderByDescending(p => p.Date).Take(requestedDays).ToList();
    }
}
