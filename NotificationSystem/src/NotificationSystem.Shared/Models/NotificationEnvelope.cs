namespace NotificationSystem.Shared.Models;

public sealed record NotificationEnvelope(
    Guid NotificationId,
    string UserId,
    NotificationChannel Channel,
    NotificationPriority Priority,
    string? BlueprintId,
    Dictionary<string, string> Payload,
    int AttemptCount,
    string CorrelationId,
    DateTimeOffset RequestedAt,
    string SourceService,
    Dictionary<string, string> Metadata);
