namespace NotificationSystem.Shared.Configuration;

public sealed class InfrastructureOptions
{
    public const string SectionName = "Infrastructure";

    public KafkaOptions Kafka { get; init; } = new();

    public RedisOptions Redis { get; init; } = new();

    public MySqlOptions MySql { get; init; } = new();

    public CassandraOptions Cassandra { get; init; } = new();

    public AuthOptions Auth { get; init; } = new();

    public RateLimitOptions RateLimit { get; init; } = new();

    public DeliveryOptions Delivery { get; init; } = new();
}

public sealed class KafkaOptions
{
    public string BootstrapServers { get; init; } = "localhost:9092";

    public string ClientId { get; init; } = "notification-system";

    public KafkaTopicOptions Topics { get; init; } = new();

    public KafkaConsumerGroups ConsumerGroups { get; init; } = new();
}

public sealed class KafkaTopicOptions
{
    public string HighPriority { get; init; } = "notifications.high";

    public string MediumPriority { get; init; } = "notifications.medium";

    public string LowPriority { get; init; } = "notifications.low";

    public string Email { get; init; } = "channel.email";

    public string Sms { get; init; } = "channel.sms";

    public string Push { get; init; } = "channel.push";

    public string Audit { get; init; } = "notifications.audit";

    public string DlqEmail { get; init; } = "dlq.email";

    public string DlqSms { get; init; } = "dlq.sms";

    public string DlqPush { get; init; } = "dlq.push";
}

public sealed class KafkaConsumerGroups
{
    public string ProcessorHigh { get; init; } = "notification-processor-high";

    public string ProcessorMedium { get; init; } = "notification-processor-medium";

    public string ProcessorLow { get; init; } = "notification-processor-low";

    public string EmailWorker { get; init; } = "notification-worker-email";

    public string SmsWorker { get; init; } = "notification-worker-sms";

    public string PushWorker { get; init; } = "notification-worker-push";

    public string AuditLogger { get; init; } = "notification-audit-logger";
}

public sealed class RedisOptions
{
    public string ConnectionString { get; init; } = "localhost:6379";

    public int BlueprintCacheTtlMinutes { get; init; } = 30;
}

public sealed class MySqlOptions
{
    public string ConnectionString { get; init; } = "Server=localhost;Port=3306;Database=notifications;User=root;Password=root;";
}

public sealed class CassandraOptions
{
    public string ContactPoints { get; init; } = "localhost";

    public int Port { get; init; } = 9042;

    public string Keyspace { get; init; } = "notification_audit";

    public string Datacenter { get; init; } = "datacenter1";
}

public sealed class AuthOptions
{
    public string Issuer { get; init; } = "notification-dev-issuer";

    public string Audience { get; init; } = "notification-internal-clients";

    public string SigningKey { get; init; } = "super-secret-dev-signing-key-with-at-least-32-chars";
}

public sealed class RateLimitOptions
{
    public int MaxPerWindow { get; init; } = 2;

    public int WindowHours { get; init; } = 24;
}

public sealed class DeliveryOptions
{
    public int MaxRetries { get; init; } = 3;

    public int BaseDelaySeconds { get; init; } = 2;
}
