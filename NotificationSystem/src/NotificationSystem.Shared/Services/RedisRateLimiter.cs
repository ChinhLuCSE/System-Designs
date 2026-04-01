using Microsoft.Extensions.Options;
using NotificationSystem.Shared.Abstractions;
using NotificationSystem.Shared.Configuration;
using NotificationSystem.Shared.Models;
using StackExchange.Redis;

namespace NotificationSystem.Shared.Services;

public sealed class RedisRateLimiter(
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<InfrastructureOptions> options,
    TimeProvider timeProvider) : IRateLimiter
{
    private readonly IDatabase database = connectionMultiplexer.GetDatabase();

    public async Task<RateLimitResult> EvaluateAsync(NotificationEnvelope envelope, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var window = TimeSpan.FromHours(options.Value.RateLimit.WindowHours);
        var windowEndsAt = now.Add(window);
        var key = $"rate-limit:{envelope.UserId}:{envelope.Channel}:{now:yyyyMMddHH}";

        var currentCount = await database.StringIncrementAsync(key);
        if (currentCount == 1)
        {
            await database.KeyExpireAsync(key, window);
        }

        var limit = options.Value.RateLimit.MaxPerWindow;
        return new RateLimitResult(currentCount <= limit, (int)currentCount, limit, windowEndsAt);
    }
}
