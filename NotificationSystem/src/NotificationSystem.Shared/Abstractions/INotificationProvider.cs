using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Abstractions;

public interface INotificationProvider
{
    NotificationChannel Channel { get; }

    Task<ProviderDeliveryResult> DeliverAsync(NotificationEnvelope envelope, DeviceSetting destination, NotificationBlueprint? blueprint, CancellationToken cancellationToken);
}
