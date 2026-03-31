using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TinyURL.Api.Abstractions;
using TinyURL.Api.Options;

namespace TinyURL.Api.Infrastructure;

public sealed class RedisUrlCache(
    IOptions<RedisOptions> options,
    IOptions<TinyUrlOptions> tinyUrlOptions) : IUrlCache, IAsyncDisposable
{
    private readonly RedisOptions _options = options.Value;
    private readonly TinyUrlOptions _tinyUrlOptions = tinyUrlOptions.Value;
    private readonly Lazy<Task<ConnectionMultiplexer>> _connectionFactory = new(() => ConnectionMultiplexer.ConnectAsync(options.Value.Configuration));

    public async Task<string?> GetAsync(string shortCode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = await GetDatabaseAsync();
        return await db.StringGetAsync(BuildKey(shortCode));
    }

    public async Task SetAsync(string shortCode, string longUrl, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = await GetDatabaseAsync();
        await db.StringSetAsync(BuildKey(shortCode), longUrl, _options.DefaultTtl ?? TimeSpan.FromHours(_tinyUrlOptions.CacheTtlHours));
    }

    public async ValueTask DisposeAsync()
    {
        if (_connectionFactory.IsValueCreated)
        {
            var connection = await _connectionFactory.Value;
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    private async Task<IDatabase> GetDatabaseAsync()
    {
        var connection = await _connectionFactory.Value;
        return connection.GetDatabase();
    }

    private static string BuildKey(string shortCode) => $"tinyurl:{shortCode}";
}
