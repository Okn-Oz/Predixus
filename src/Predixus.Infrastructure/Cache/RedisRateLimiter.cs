using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Predixus.Infrastructure.Cache;

public class RedisRateLimiter(IConnectionMultiplexer redis, ILogger<RedisRateLimiter> logger)
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<bool> IsAllowedAsync(
        string ip,
        string endpoint,
        int maxRequests,
        TimeSpan window,
        CancellationToken ct = default)
    {
        var key = $"ratelimit:{ip}:{endpoint}";

        try
        {
            var count = await _db.StringIncrementAsync(key);

            if (count == 1)
                await _db.KeyExpireAsync(key, window);

            return count <= maxRequests;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis rate limit hatası: {Key} — istek geçiriliyor", key);
            return true; // Redis çökerse isteği engelleme
        }
    }
}
