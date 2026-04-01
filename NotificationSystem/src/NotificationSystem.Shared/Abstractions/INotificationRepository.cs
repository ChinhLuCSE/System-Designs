using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Abstractions;

public interface INotificationRepository
{
    Task EnsureSchemaAsync(CancellationToken cancellationToken);

    Task SeedAsync(CancellationToken cancellationToken);

    Task CreateAsync(NotificationRecord notification, CancellationToken cancellationToken);

    Task<NotificationRecord?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken);

    Task UpdateStatusAsync(Guid notificationId, NotificationStatus status, int? attemptCount, string? lastError, DateTimeOffset? sentAt, CancellationToken cancellationToken);

    Task<DeviceSetting?> GetActiveDeviceSettingAsync(string userId, NotificationChannel channel, CancellationToken cancellationToken);

    Task<NotificationBlueprint?> GetBlueprintAsync(string blueprintId, CancellationToken cancellationToken);
}
