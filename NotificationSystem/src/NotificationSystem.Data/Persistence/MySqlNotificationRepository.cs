using System.Data;
using Dapper;
using NotificationSystem.Shared.Abstractions;
using NotificationSystem.Shared.Models;

namespace NotificationSystem.Data.Persistence;

public sealed class MySqlNotificationRepository(MySqlConnectionFactory connectionFactory) : INotificationRepository
{
    public async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS users (
                id VARCHAR(64) NOT NULL PRIMARY KEY,
                email VARCHAR(255) NULL,
                phone_number VARCHAR(32) NULL,
                created_at DATETIME(6) NOT NULL
            );

            CREATE TABLE IF NOT EXISTS device_settings (
                id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                user_id VARCHAR(64) NOT NULL,
                notification_channel VARCHAR(32) NOT NULL,
                destination VARCHAR(255) NOT NULL,
                device_token VARCHAR(255) NULL,
                is_active BIT NOT NULL,
                created_at DATETIME(6) NOT NULL,
                INDEX idx_device_settings_user_channel (user_id, notification_channel)
            );

            CREATE TABLE IF NOT EXISTS notification_blueprints (
                id VARCHAR(64) NOT NULL PRIMARY KEY,
                channel VARCHAR(32) NOT NULL,
                subject VARCHAR(255) NOT NULL,
                content TEXT NOT NULL,
                is_active BIT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS notifications (
                id CHAR(36) NOT NULL PRIMARY KEY,
                user_id VARCHAR(64) NOT NULL,
                channel VARCHAR(32) NOT NULL,
                priority VARCHAR(32) NOT NULL,
                blueprint_id VARCHAR(64) NULL,
                payload_json JSON NOT NULL,
                source_service VARCHAR(128) NOT NULL,
                dedupe_key VARCHAR(255) NULL,
                correlation_id VARCHAR(64) NOT NULL,
                status VARCHAR(32) NOT NULL,
                attempt_count INT NOT NULL,
                last_error TEXT NULL,
                created_at DATETIME(6) NOT NULL,
                updated_at DATETIME(6) NOT NULL,
                sent_at DATETIME(6) NULL,
                INDEX idx_notifications_user (user_id),
                INDEX idx_notifications_status (status)
            );
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT IGNORE INTO users (id, email, phone_number, created_at) VALUES
            ('user-1', 'user-1@example.com', '+84901111001', UTC_TIMESTAMP(6)),
            ('user-2', 'transient-fail@example.com', '+84901111002', UTC_TIMESTAMP(6)),
            ('user-3', 'permanent-fail@example.com', '+84901111003', UTC_TIMESTAMP(6));

            INSERT IGNORE INTO device_settings (id, user_id, notification_channel, destination, device_token, is_active, created_at) VALUES
            (1, 'user-1', 'Email', 'user-1@example.com', NULL, 1, UTC_TIMESTAMP(6)),
            (2, 'user-1', 'Sms', '+84901111001', NULL, 1, UTC_TIMESTAMP(6)),
            (3, 'user-1', 'Push', 'user-1-device', 'push-user-1', 1, UTC_TIMESTAMP(6)),
            (4, 'user-2', 'Email', 'transient-fail@example.com', NULL, 1, UTC_TIMESTAMP(6)),
            (5, 'user-2', 'Sms', '+84901111002', NULL, 1, UTC_TIMESTAMP(6)),
            (6, 'user-3', 'Email', 'permanent-fail@example.com', NULL, 1, UTC_TIMESTAMP(6));

            INSERT IGNORE INTO notification_blueprints (id, channel, subject, content, is_active) VALUES
            ('welcome-email', 'Email', 'Welcome to NotificationSystem', 'Hello {{userId}}, welcome aboard.', 1),
            ('otp-sms', 'Sms', 'OTP', 'Your OTP is {{otp}}', 1),
            ('billing-push', 'Push', 'Billing update', 'A billing event has been created.', 1);
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task CreateAsync(NotificationRecord notification, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO notifications (
                id, user_id, channel, priority, blueprint_id, payload_json, source_service, dedupe_key,
                correlation_id, status, attempt_count, last_error, created_at, updated_at, sent_at)
            VALUES (
                @Id, @UserId, @Channel, @Priority, @BlueprintId, @PayloadJson, @SourceService, @DedupeKey,
                @CorrelationId, @Status, @AttemptCount, @LastError, @CreatedAt, @UpdatedAt, @SentAt);
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = notification.Id.ToString(),
            notification.UserId,
            Channel = notification.Channel.ToString(),
            Priority = notification.Priority.ToString(),
            notification.BlueprintId,
            notification.PayloadJson,
            notification.SourceService,
            notification.DedupeKey,
            notification.CorrelationId,
            Status = notification.Status.ToString(),
            notification.AttemptCount,
            notification.LastError,
            CreatedAt = notification.CreatedAt.UtcDateTime,
            UpdatedAt = notification.UpdatedAt.UtcDateTime,
            SentAt = notification.SentAt?.UtcDateTime
        }, cancellationToken: cancellationToken));
    }

    public async Task<NotificationRecord?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                id,
                user_id AS UserId,
                channel AS Channel,
                priority AS Priority,
                blueprint_id AS BlueprintId,
                payload_json AS PayloadJson,
                source_service AS SourceService,
                dedupe_key AS DedupeKey,
                correlation_id AS CorrelationId,
                status AS Status,
                attempt_count AS AttemptCount,
                last_error AS LastError,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt,
                sent_at AS SentAt
            FROM notifications
            WHERE id = @Id
            LIMIT 1;
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var reader = await connection.ExecuteReaderAsync(new CommandDefinition(sql, new { Id = notificationId.ToString() }, cancellationToken: cancellationToken));
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new NotificationRecord(
            Guid.Parse(reader.GetString("id")),
            reader.GetString("UserId"),
            Enum.Parse<NotificationChannel>(reader.GetString("Channel")),
            Enum.Parse<NotificationPriority>(reader.GetString("Priority")),
            reader.IsDBNull("BlueprintId") ? null : reader.GetString("BlueprintId"),
            reader.GetString("PayloadJson"),
            reader.GetString("SourceService"),
            reader.IsDBNull("DedupeKey") ? null : reader.GetString("DedupeKey"),
            reader.GetString("CorrelationId"),
            Enum.Parse<NotificationStatus>(reader.GetString("Status")),
            reader.GetInt32("AttemptCount"),
            reader.IsDBNull("LastError") ? null : reader.GetString("LastError"),
            new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime("CreatedAt"), DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime("UpdatedAt"), DateTimeKind.Utc)),
            reader.IsDBNull("SentAt") ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime("SentAt"), DateTimeKind.Utc)));
    }

    public async Task UpdateStatusAsync(Guid notificationId, NotificationStatus status, int? attemptCount, string? lastError, DateTimeOffset? sentAt, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE notifications
            SET status = @Status,
                attempt_count = COALESCE(@AttemptCount, attempt_count),
                last_error = @LastError,
                updated_at = UTC_TIMESTAMP(6),
                sent_at = @SentAt
            WHERE id = @Id;
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = notificationId.ToString(),
            Status = status.ToString(),
            AttemptCount = attemptCount,
            LastError = lastError,
            SentAt = sentAt?.UtcDateTime
        }, cancellationToken: cancellationToken));
    }

    public async Task<DeviceSetting?> GetActiveDeviceSettingAsync(string userId, NotificationChannel channel, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                id AS Id,
                user_id AS UserId,
                notification_channel AS Channel,
                destination AS Destination,
                device_token AS DeviceToken,
                is_active AS IsActive
            FROM device_settings
            WHERE user_id = @UserId
              AND notification_channel = @Channel
              AND is_active = 1
            ORDER BY id
            LIMIT 1;
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var reader = await connection.ExecuteReaderAsync(new CommandDefinition(sql, new { UserId = userId, Channel = channel.ToString() }, cancellationToken: cancellationToken));
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new DeviceSetting(
            reader.GetInt64("Id"),
            reader.GetString("UserId"),
            Enum.Parse<NotificationChannel>(reader.GetString("Channel")),
            reader.GetString("Destination"),
            reader.IsDBNull("DeviceToken") ? null : reader.GetString("DeviceToken"),
            reader.GetBoolean("IsActive"));
    }

    public async Task<NotificationBlueprint?> GetBlueprintAsync(string blueprintId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                id AS Id,
                channel AS Channel,
                subject AS Subject,
                content AS Content,
                is_active AS IsActive
            FROM notification_blueprints
            WHERE id = @Id
            LIMIT 1;
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var reader = await connection.ExecuteReaderAsync(new CommandDefinition(sql, new { Id = blueprintId }, cancellationToken: cancellationToken));
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new NotificationBlueprint(
            reader.GetString("Id"),
            Enum.Parse<NotificationChannel>(reader.GetString("Channel")),
            reader.GetString("Subject"),
            reader.GetString("Content"),
            reader.GetBoolean("IsActive"));
    }
}

internal static class DataRecordExtensions
{
    public static string GetString(this IDataRecord record, string name)
    {
        var value = record.GetValue(record.GetOrdinal(name));
        return value switch
        {
            Guid guid => guid.ToString(),
            string text => text,
            _ => Convert.ToString(value)!,
        };
    }

    public static int GetInt32(this IDataRecord record, string name) => record.GetInt32(record.GetOrdinal(name));

    public static long GetInt64(this IDataRecord record, string name) => record.GetInt64(record.GetOrdinal(name));

    public static bool GetBoolean(this IDataRecord record, string name)
    {
        var value = record.GetValue(record.GetOrdinal(name));
        return value switch
        {
            bool boolean => boolean,
            byte tiny => tiny != 0,
            sbyte signedTiny => signedTiny != 0,
            short small => small != 0,
            ushort unsignedSmall => unsignedSmall != 0,
            int integer => integer != 0,
            long big => big != 0,
            ulong unsignedBig => unsignedBig != 0,
            _ => Convert.ToBoolean(value)
        };
    }

    public static DateTime GetDateTime(this IDataRecord record, string name) => record.GetDateTime(record.GetOrdinal(name));

    public static bool IsDBNull(this IDataRecord record, string name) => record.IsDBNull(record.GetOrdinal(name));
}
