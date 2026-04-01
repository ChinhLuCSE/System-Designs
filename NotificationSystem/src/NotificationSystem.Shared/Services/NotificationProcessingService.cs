using Microsoft.Extensions.Options;
using NotificationSystem.Shared.Abstractions;
using NotificationSystem.Shared.Configuration;
using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Services;

public sealed class NotificationProcessingService(
    INotificationRepository notificationRepository,
    IRateLimiter rateLimiter,
    IBlueprintCache blueprintCache,
    IMessagePublisher messagePublisher,
    IOptions<InfrastructureOptions> infrastructureOptions,
    TimeProvider timeProvider) : INotificationProcessingService
{
    public async Task ProcessAsync(NotificationEnvelope envelope, CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.GetByIdAsync(envelope.NotificationId, cancellationToken);
        if (notification is null || notification.Status is NotificationStatus.Sent or NotificationStatus.Cancelled or NotificationStatus.DeadLettered)
        {
            return;
        }

        var deviceSetting = await notificationRepository.GetActiveDeviceSettingAsync(envelope.UserId, envelope.Channel, cancellationToken);
        if (deviceSetting is null)
        {
            await notificationRepository.UpdateStatusAsync(envelope.NotificationId, NotificationStatus.Cancelled, envelope.AttemptCount, "User has not consented to this channel.", sentAt: null, cancellationToken);
            await PublishAuditAsync(envelope, NotificationStatus.Cancelled, AuditEventType.Cancelled, envelope.AttemptCount, "Notification cancelled because the recipient has no active consent.", cancellationToken);
            return;
        }

        var rateLimitResult = await rateLimiter.EvaluateAsync(envelope, cancellationToken);
        if (!rateLimitResult.Allowed)
        {
            await notificationRepository.UpdateStatusAsync(envelope.NotificationId, NotificationStatus.Cancelled, envelope.AttemptCount, $"Rate limit exceeded until {rateLimitResult.WindowEndsAt:O}.", sentAt: null, cancellationToken);
            await PublishAuditAsync(envelope, NotificationStatus.Cancelled, AuditEventType.Cancelled, envelope.AttemptCount, "Notification cancelled because rate limit was exceeded.", cancellationToken);
            return;
        }

        if (!string.IsNullOrWhiteSpace(envelope.BlueprintId))
        {
            var blueprint = await blueprintCache.GetAsync(envelope.BlueprintId, cancellationToken) ?? await notificationRepository.GetBlueprintAsync(envelope.BlueprintId, cancellationToken);
            if (blueprint is not null)
            {
                await blueprintCache.SetAsync(blueprint, cancellationToken);
            }
        }

        await notificationRepository.UpdateStatusAsync(envelope.NotificationId, NotificationStatus.Processing, envelope.AttemptCount, lastError: null, sentAt: null, cancellationToken);
        await PublishAuditAsync(envelope, NotificationStatus.Processing, AuditEventType.Processing, envelope.AttemptCount, "Notification is ready for channel delivery.", cancellationToken);
        await messagePublisher.PublishAsync(TopicNameResolver.ResolveChannelTopic(envelope.Channel, infrastructureOptions.Value.Kafka.Topics), envelope.NotificationId.ToString("N"), envelope, cancellationToken);
    }

    private Task PublishAuditAsync(NotificationEnvelope envelope, NotificationStatus status, AuditEventType eventType, int attemptCount, string details, CancellationToken cancellationToken)
    {
        var auditEvent = new AuditEvent(
            envelope.NotificationId,
            envelope.CorrelationId,
            envelope.SourceService,
            envelope.Channel,
            envelope.Priority,
            status,
            eventType,
            attemptCount,
            details,
            timeProvider.GetUtcNow());

        return messagePublisher.PublishAsync(infrastructureOptions.Value.Kafka.Topics.Audit, envelope.NotificationId.ToString("N"), auditEvent, cancellationToken);
    }
}
