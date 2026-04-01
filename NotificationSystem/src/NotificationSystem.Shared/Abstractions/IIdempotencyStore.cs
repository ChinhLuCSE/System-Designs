namespace NotificationSystem.Shared.Abstractions;

public interface IIdempotencyStore
{
    Task<bool> TryReserveAsync(string key, string notificationId, TimeSpan ttl, CancellationToken cancellationToken);

    Task<string?> GetNotificationIdAsync(string key, CancellationToken cancellationToken);
}
