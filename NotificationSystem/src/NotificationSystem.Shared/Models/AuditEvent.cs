namespace NotificationSystem.Shared.Models;

public sealed record AuditEvent(
    Guid NotificationId,
    string CorrelationId,
    string SourceService,
    NotificationChannel Channel,
    NotificationPriority Priority,
    NotificationStatus Status,
    AuditEventType EventType,
    int AttemptCount,
    string Details,
    DateTimeOffset OccurredAt);
