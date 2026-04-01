namespace NotificationSystem.Shared.Models;

public sealed record NotificationRequest(
    string UserId,
    NotificationChannel Channel,
    NotificationPriority Priority,
    string? BlueprintId,
    Dictionary<string, string>? Payload,
    string? DedupeKey,
    Dictionary<string, string>? Metadata,
    string SourceService);

public sealed record NotificationAcceptedResponse(Guid NotificationId, NotificationStatus Status, string CorrelationId);

public sealed record NotificationDetailsResponse(
    Guid NotificationId,
    string UserId,
    NotificationChannel Channel,
    NotificationPriority Priority,
    NotificationStatus Status,
    int AttemptCount,
    string? LastError,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt,
    AuditEvent? LatestAuditEvent);
