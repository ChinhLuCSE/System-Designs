namespace NotificationSystem.Shared.Models;

public sealed record NotificationRecord(
    Guid Id,
    string UserId,
    NotificationChannel Channel,
    NotificationPriority Priority,
    string? BlueprintId,
    string PayloadJson,
    string SourceService,
    string? DedupeKey,
    string CorrelationId,
    NotificationStatus Status,
    int AttemptCount,
    string? LastError,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? SentAt);
