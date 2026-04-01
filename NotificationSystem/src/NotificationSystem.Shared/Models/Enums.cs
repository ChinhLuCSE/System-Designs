namespace NotificationSystem.Shared.Models;

public enum NotificationChannel
{
    Email = 1,
    Sms = 2,
    Push = 3
}

public enum NotificationPriority
{
    High = 1,
    Medium = 2,
    Low = 3
}

public enum NotificationStatus
{
    Pending = 1,
    Queued = 2,
    Processing = 3,
    Sent = 4,
    Failed = 5,
    Cancelled = 6,
    DeadLettered = 7
}

public enum AuditEventType
{
    Accepted = 1,
    Queued = 2,
    Processing = 3,
    Delivered = 4,
    DeliveryFailed = 5,
    Cancelled = 6,
    DeadLettered = 7,
    RetryScheduled = 8
}
