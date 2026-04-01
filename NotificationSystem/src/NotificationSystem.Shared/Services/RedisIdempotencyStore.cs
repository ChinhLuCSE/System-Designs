using NotificationSystem.Shared.Abstractions;
using StackExchange.Redis;

namespace NotificationSystem.Shared.Services;

public sealed class RedisIdempotencyStore(IConnectionMultiplexer connectionMultiplexer) : IIdempotencyStore
{
    private readonly IDatabase database = connectionMultiplexer.GetDatabase();

    public async Task<string?> GetNotificationIdAsync(string key, CancellationToken cancellationToken)
    {
        var value = await database.StringGetAsync(CacheKey(key));
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public Task<bool> TryReserveAsync(string key, string notificationId, TimeSpan ttl, CancellationToken cancellationToken)
        => database.StringSetAsync(CacheKey(key), notificationId, ttl, When.NotExists);

    private static string CacheKey(string key) => $"idempotency:{key}";
}
