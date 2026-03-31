using Cassandra;
using Microsoft.Extensions.Options;
using TinyURL.Api.Abstractions;
using TinyURL.Api.Domain;
using TinyURL.Api.Options;
using CassandraSession = Cassandra.ISession;

namespace TinyURL.Api.Infrastructure;

public sealed class CassandraUrlRepository(
    IOptions<CassandraOptions> options,
    ILogger<CassandraUrlRepository> logger) : IUrlRepository, IAsyncDisposable
{
    private readonly CassandraOptions _options = options.Value;
    private readonly ILogger<CassandraUrlRepository> _logger = logger;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    private ICluster? _cluster;
    private CassandraSession? _session;
    private PreparedStatement? _insertStatement;
    private PreparedStatement? _selectStatement;
    private bool _initialized;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            _cluster = await ExecuteWithRetryAsync(async () =>
            {
                var builder = new Builder()
                    .AddContactPoints(_options.ContactPoints)
                    .WithPort(_options.Port);

                return await Task.FromResult(builder.Build());
            }, "connect to Cassandra cluster", cancellationToken);

            _session = await ExecuteWithRetryAsync(
                () => _cluster.ConnectAsync(),
                "open Cassandra session",
                cancellationToken);

            await ExecuteWithRetryAsync(
                () => _session.ExecuteAsync(new SimpleStatement(
                    $"CREATE KEYSPACE IF NOT EXISTS {_options.Keyspace} " +
                    "WITH replication = { 'class': 'SimpleStrategy', 'replication_factor': 1 }")),
                "create Cassandra keyspace",
                cancellationToken);

            await ExecuteWithRetryAsync(
                () => _session.ExecuteAsync(new SimpleStatement($"USE {_options.Keyspace}")),
                "switch Cassandra keyspace",
                cancellationToken);

            await ExecuteWithRetryAsync(
                () => _session.ExecuteAsync(new SimpleStatement($"""
                    CREATE TABLE IF NOT EXISTS {_options.TableName} (
                        short_code text PRIMARY KEY,
                        id uuid,
                        long_url text,
                        created_at timestamp
                    )
                    """)),
                "create Cassandra table",
                cancellationToken);

            _insertStatement = await ExecuteWithRetryAsync(
                () => _session.PrepareAsync($"""
                    INSERT INTO {_options.TableName} (short_code, id, long_url, created_at)
                    VALUES (?, ?, ?, ?)
                    """),
                "prepare insert statement",
                cancellationToken);

            _selectStatement = await ExecuteWithRetryAsync(
                () => _session.PrepareAsync($"""
                    SELECT short_code, id, long_url, created_at
                    FROM {_options.TableName}
                    WHERE short_code = ?
                    """),
                "prepare select statement",
                cancellationToken);

            _initialized = true;
            _logger.LogInformation("Cassandra repository initialized for keyspace {Keyspace}.", _options.Keyspace);
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async Task<ShortUrlRecord?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken)
    {
        await InitializeAsync(cancellationToken);

        var rowSet = await ExecuteWithRetryAsync(
            () => _session!.ExecuteAsync(_selectStatement!.Bind(shortCode)),
            "read URL mapping from Cassandra",
            cancellationToken);

        var row = rowSet.SingleOrDefault();
        if (row is null)
        {
            return null;
        }

        return new ShortUrlRecord(
            row.GetValue<Guid>("id"),
            row.GetValue<string>("long_url"),
            row.GetValue<string>("short_code"),
            new DateTimeOffset(DateTime.SpecifyKind(row.GetValue<DateTime>("created_at"), DateTimeKind.Utc)));
    }

    public async Task SaveAsync(ShortUrlRecord record, CancellationToken cancellationToken)
    {
        await InitializeAsync(cancellationToken);

        var statement = _insertStatement!
            .Bind(record.ShortCode, record.Id, record.LongUrl, record.CreatedAtUtc.UtcDateTime)
            .SetConsistencyLevel(ConsistencyLevel.Quorum);

        await ExecuteWithRetryAsync(
            () => _session!.ExecuteAsync(statement),
            "write URL mapping to Cassandra",
            cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_session is not null)
        {
            await _session.ShutdownAsync();
        }

        if (_cluster is not null)
        {
            await _cluster.ShutdownAsync();
        }

        _initializationLock.Dispose();
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
}
