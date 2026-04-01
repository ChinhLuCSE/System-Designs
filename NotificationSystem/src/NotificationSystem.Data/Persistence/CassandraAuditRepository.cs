using Cassandra;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationSystem.Shared.Abstractions;
using NotificationSystem.Shared.Configuration;
using NotificationSystem.Shared.Models;

namespace NotificationSystem.Data.Persistence;

public sealed class CassandraAuditRepository(ICluster cluster, IOptions<InfrastructureOptions> options, ILogger<CassandraAuditRepository> logger) : IAuditRepository, IAsyncDisposable
{
    private ISession? session;

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        session ??= await ConnectWithRetryAsync(null, cancellationToken);

        var createKeyspace = $"CREATE KEYSPACE IF NOT EXISTS {options.Value.Cassandra.Keyspace} " +
            "WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1};";

        await session.ExecuteAsync(new SimpleStatement(createKeyspace)).WaitAsync(cancellationToken);

        session = await ConnectWithRetryAsync(options.Value.Cassandra.Keyspace, cancellationToken);

        await session.ExecuteAsync(new SimpleStatement("""
            CREATE TABLE IF NOT EXISTS audit_events (
                notification_id uuid,
                occurred_at timestamp,
                event_id timeuuid,
                correlation_id text,
                source_service text,
                channel text,
                priority text,
                status text,
                event_type text,
                attempt_count int,
                details text,
                PRIMARY KEY ((notification_id), occurred_at, event_id)
            ) WITH CLUSTERING ORDER BY (occurred_at DESC, event_id DESC);
            """)).WaitAsync(cancellationToken);
    }

    public async Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        session ??= await ConnectWithRetryAsync(options.Value.Cassandra.Keyspace, cancellationToken);

        var statement = new SimpleStatement("""
            INSERT INTO audit_events (
                notification_id, occurred_at, event_id, correlation_id, source_service, channel, priority, status, event_type, attempt_count, details)
            VALUES (?, ?, now(), ?, ?, ?, ?, ?, ?, ?, ?);
            """,
            auditEvent.NotificationId,
            auditEvent.OccurredAt.UtcDateTime,
            auditEvent.CorrelationId,
            auditEvent.SourceService,
            auditEvent.Channel.ToString(),
            auditEvent.Priority.ToString(),
            auditEvent.Status.ToString(),
            auditEvent.EventType.ToString(),
            auditEvent.AttemptCount,
            auditEvent.Details);

        await session.ExecuteAsync(statement).WaitAsync(cancellationToken);
    }

    public async Task<AuditEvent?> GetLatestAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        session ??= await ConnectWithRetryAsync(options.Value.Cassandra.Keyspace, cancellationToken);

        var statement = new SimpleStatement("""
            SELECT notification_id, occurred_at, correlation_id, source_service, channel, priority, status, event_type, attempt_count, details
            FROM audit_events
            WHERE notification_id = ?
            LIMIT 1;
            """, notificationId);

        var rowSet = await session.ExecuteAsync(statement).WaitAsync(cancellationToken);
        var row = rowSet.SingleOrDefault();
        if (row is null)
        {
            return null;
        }

        return new AuditEvent(
            row.GetValue<Guid>("notification_id"),
            row.GetValue<string>("correlation_id"),
            row.GetValue<string>("source_service"),
            Enum.Parse<NotificationChannel>(row.GetValue<string>("channel")),
            Enum.Parse<NotificationPriority>(row.GetValue<string>("priority")),
            Enum.Parse<NotificationStatus>(row.GetValue<string>("status")),
            Enum.Parse<AuditEventType>(row.GetValue<string>("event_type")),
            row.GetValue<int>("attempt_count"),
            row.GetValue<string>("details"),
            new DateTimeOffset(DateTime.SpecifyKind(row.GetValue<DateTime>("occurred_at"), DateTimeKind.Utc)));
    }

    public async ValueTask DisposeAsync()
    {
        if (session is not null)
        {
            await session.ShutdownAsync();
        }

        cluster.Dispose();
    }

    private async Task<ISession> ConnectWithRetryAsync(string? keyspace, CancellationToken cancellationToken)
    {
        const int maxAttempts = 20;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return keyspace is null
                    ? await cluster.ConnectAsync().WaitAsync(cancellationToken)
                    : await cluster.ConnectAsync(keyspace).WaitAsync(cancellationToken);
            }
            catch (NoHostAvailableException ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Cassandra connection attempt {Attempt}/{MaxAttempts} failed. Retrying.", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }

        throw new InvalidOperationException("Cassandra connection retry loop exited unexpectedly.");
    }
}
