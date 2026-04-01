using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Abstractions;

public interface INotificationProcessingService
{
    Task ProcessAsync(NotificationEnvelope envelope, CancellationToken cancellationToken);
}
