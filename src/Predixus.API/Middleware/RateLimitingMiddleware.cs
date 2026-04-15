using System.Net;
using System.Text.Json;
using Predixus.Infrastructure.Cache;

namespace Predixus.API.Middleware;

public class RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
{
    private const int MaxRequests = 30;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public async Task InvokeAsync(HttpContext context)
    {
        var rateLimiter = context.RequestServices.GetService<RedisRateLimiter>();

        if (rateLimiter is not null)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var endpoint = context.Request.Path.Value ?? "/";

            var allowed = await rateLimiter.IsAllowedAsync(ip, endpoint, MaxRequests, Window);

            if (!allowed)
            {
                logger.LogWarning("Rate limit aşıldı: {IP} → {Endpoint}", ip, endpoint);

                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "application/json";

                var response = new { status = 429, message = "Çok fazla istek gönderildi. Lütfen bekleyin." };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                return;
            }
        }

        await next(context);
    }
}
