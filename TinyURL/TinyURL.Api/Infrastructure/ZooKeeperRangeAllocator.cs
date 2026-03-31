using System.Text;
using Microsoft.Extensions.Options;
using org.apache.zookeeper;
using TinyURL.Api.Abstractions;
using TinyURL.Api.Options;

namespace TinyURL.Api.Infrastructure;

public sealed class ZooKeeperRangeAllocator(
    IOptions<ZooKeeperOptions> options,
    ILogger<ZooKeeperRangeAllocator> logger,
    ILogger<ZooKeeperConnectionWatcher> watcherLogger) : IRangeAllocator, IAsyncDisposable
{
    private const string RootPath = "/tinyurl";
    private const string CounterPath = "/tinyurl/range-counter";

    private readonly ZooKeeperOptions _options = options.Value;
    private readonly ILogger<ZooKeeperRangeAllocator> _logger = logger;
    private readonly SemaphoreSlim _rangeLock = new(1, 1);
    private readonly Lazy<ZooKeeper> _zooKeeperFactory = new(() => new ZooKeeper(
        options.Value.ConnectionString,
        options.Value.SessionTimeoutMs,
        new ZooKeeperConnectionWatcher(watcherLogger),
        false));

    private RangeReservation? _currentRange;

    public async Task WarmupAsync(CancellationToken cancellationToken)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await EnsureNodesAsync(cancellationToken);
            await EnsureCurrentRangeAsync(cancellationToken);
            return true;
        }, "warm up ZooKeeper range allocator", cancellationToken);
    }

    public async Task<long> GetNextIdAsync(CancellationToken cancellationToken)
    {
        await _rangeLock.WaitAsync(cancellationToken);
        try
        {
            await EnsureCurrentRangeAsync(cancellationToken);

            var nextId = _currentRange!.NextValue;
            if (nextId > _currentRange.EndInclusive)
            {
                _currentRange = await AllocateRangeAsync(cancellationToken);
                nextId = _currentRange.NextValue;
            }

            _currentRange = _currentRange with { NextValue = nextId + 1 };
            return nextId;
        }
        finally
        {
            _rangeLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_zooKeeperFactory.IsValueCreated)
        {
            await _zooKeeperFactory.Value.closeAsync();
        }

        _rangeLock.Dispose();
    }

    private async Task EnsureCurrentRangeAsync(CancellationToken cancellationToken)
    {
        if (_currentRange is not null && _currentRange.NextValue <= _currentRange.EndInclusive)
        {
            return;
        }

        _currentRange = await AllocateRangeAsync(cancellationToken);
    }

    private async Task<RangeReservation> AllocateRangeAsync(CancellationToken cancellationToken)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            await EnsureNodesAsync(cancellationToken);
            var zooKeeper = _zooKeeperFactory.Value;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var dataResult = await zooKeeper.getDataAsync(CounterPath, false);
                    var nextRangeStart = long.Parse(Encoding.UTF8.GetString(dataResult.Data));
                    var nextRangeEnd = nextRangeStart + _options.RangeSize - 1;
                    var newCounterValue = Encoding.UTF8.GetBytes((nextRangeEnd + 1).ToString());

                    await zooKeeper.setDataAsync(CounterPath, newCounterValue, dataResult.Stat.getVersion());

                    _logger.LogInformation(
                        "Allocated ZooKeeper range {Start}-{End}.",
                        nextRangeStart,
                        nextRangeEnd);

                    return new RangeReservation(nextRangeStart, nextRangeEnd, nextRangeStart);
                }
                catch (KeeperException.BadVersionException)
                {
                    _logger.LogDebug("ZooKeeper range allocation conflict detected. Retrying.");
                }
                catch (KeeperException.NoNodeException)
                {
                    await EnsureNodesAsync(cancellationToken);
                }
            }
        }, "allocate range from ZooKeeper", cancellationToken);
    }

    private async Task EnsureNodesAsync(CancellationToken cancellationToken)
    {
        var zooKeeper = _zooKeeperFactory.Value;

        await EnsureNodeExistsAsync(zooKeeper, RootPath, "root node", Encoding.UTF8.GetBytes("tinyurl"), cancellationToken);
        await EnsureNodeExistsAsync(zooKeeper, CounterPath, "range counter", Encoding.UTF8.GetBytes(_options.InitialValue.ToString()), cancellationToken);
    }

    private async Task EnsureNodeExistsAsync(
        ZooKeeper zooKeeper,
        string path,
        string description,
        byte[] data,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stat = await zooKeeper.existsAsync(path, false);
        if (stat is not null)
        {
            return;
        }

        try
        {
            await zooKeeper.createAsync(path, data, ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
            _logger.LogInformation("Created ZooKeeper {Description} at {Path}.", description, path);
        }
        catch (KeeperException.NodeExistsException)
        {
        }
    }

    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        string activity,
        CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= _options.MaxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt < _options.MaxRetries)
            {
                _logger.LogWarning(ex, "Failed to {Activity} on attempt {Attempt}. Retrying in {DelaySeconds}s.", activity, attempt, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
                delay += delay;
            }
        }

        return await operation();
    }

    private sealed record RangeReservation(long StartInclusive, long EndInclusive, long NextValue);
}
