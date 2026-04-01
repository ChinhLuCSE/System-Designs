using Microsoft.Extensions.Options;
using NotificationSystem.Shared.Abstractions;
using NotificationSystem.Shared.Configuration;
using NotificationSystem.Shared.Models;
using StackExchange.Redis;

namespace NotificationSystem.Shared.Services;

public sealed class RedisBlueprintCache(IConnectionMultiplexer connectionMultiplexer, IOptions<InfrastructureOptions> options) : IBlueprintCache
{
    private readonly IDatabase database = connectionMultiplexer.GetDatabase();

    public async Task<NotificationBlueprint?> GetAsync(string blueprintId, CancellationToken cancellationToken)
    {
        var value = await database.StringGetAsync(CacheKey(blueprintId));
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonMessageSerializer.Deserialize<NotificationBlueprint>(value!);
    }

    public Task SetAsync(NotificationBlueprint blueprint, CancellationToken cancellationToken)
    {
        var ttl = TimeSpan.FromMinutes(options.Value.Redis.BlueprintCacheTtlMinutes);
        return database.StringSetAsync(CacheKey(blueprint.Id), JsonMessageSerializer.Serialize(blueprint), ttl);
    }

    private static string CacheKey(string blueprintId) => $"blueprint:{blueprintId}";
}
