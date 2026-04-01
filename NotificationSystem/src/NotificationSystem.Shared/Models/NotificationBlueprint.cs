namespace NotificationSystem.Shared.Models;

public sealed record NotificationBlueprint(
    string Id,
    NotificationChannel Channel,
    string Subject,
    string Content,
    bool IsActive);
