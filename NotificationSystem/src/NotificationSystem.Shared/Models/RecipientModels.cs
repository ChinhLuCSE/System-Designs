namespace NotificationSystem.Shared.Models;

public sealed record DeviceSetting(
    long Id,
    string UserId,
    NotificationChannel Channel,
    string Destination,
    string? DeviceToken,
    bool IsActive);
