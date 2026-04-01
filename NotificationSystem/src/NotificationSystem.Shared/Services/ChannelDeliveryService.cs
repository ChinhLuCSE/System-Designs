using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationSystem.Shared.Abstractions;
using NotificationSystem.Shared.Configuration;
using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Services;

public sealed class ChannelDeliveryService(
    IEnumerable<INotificationProvider> providers,
    INotificationRepository notificationRepository,
    IBlueprintCache blueprintCache,
    IMessagePublisher messagePublisher,
    IOptions<InfrastructureOptions> infrastructureOptions,
    ILogger<ChannelDeliveryService> logger,
    TimeProvider timeProvider)
{
    private readonly Dictionary<NotificationChannel, INotificationProvider> providerMap = providers.ToDictionary(provider => provider.Channel);

    public async Task DeliverAsync(NotificationEnvelope envelope, CancellationToken cancellationToken)
    {
        if (!providerMap.TryGetValue(envelope.Channel, out var provider))
        {
            throw new InvalidOperationException($"No provider registered for channel '{envelope.Channel}'.");
        }

        var device = await notificationRepository.GetActiveDeviceSettingAsync(envelope.UserId, envelope.Channel, cancellationToken);
        if (device is null)
        {
            await notificationRepository.UpdateStatusAsync(envelope.NotificationId, NotificationStatus.Cancelled, envelope.AttemptCount, "No active device or destination found for channel.", sentAt: null, cancellationToken);
            await PublishAuditAsync(envelope, NotificationStatus.Cancelled, AuditEventType.Cancelled, envelope.AttemptCount, "Notification cancelled because no active destination was found.", cancellationToken);
            return;
        }

        NotificationBlueprint? blueprint = null;
        if (!string.IsNullOrWhiteSpace(envelope.BlueprintId))
        {
            blueprint = await blueprintCache.GetAsync(envelope.BlueprintId, cancellationToken);
        }

        var result = await provider.DeliverAsync(envelope, device, blueprint, cancellationToken);
        if (result.Succeeded)
        {
            await notificationRepository.UpdateStatusAsync(envelope.NotificationId, NotificationStatus.Sent, envelope.AttemptCount, lastError: null, sentAt: timeProvider.GetUtcNow(), cancellationToken);
            await PublishAuditAsync(envelope, NotificationStatus.Sent, AuditEventType.Delivered, envelope.AttemptCount, result.ProviderReference ?? "Delivered by fake provider.", cancellationToken);
            return;
        }

        var options = infrastructureOptions.Value;
        var nextAttempt = envelope.AttemptCount + 1;
        if (result.IsTransientFailure && nextAttempt < options.Delivery.MaxRetries)
        {
            var delay = TimeSpan.FromSeconds(Math.Pow(2, nextAttempt) * options.Delivery.BaseDelaySeconds);
            logger.LogWarning("Transient failure for notification {NotificationId}. Scheduling retry {Attempt} after {Delay}.", envelope.NotificationId, nextAttempt, delay);
            await Task.Delay(delay, cancellationToken);

            var retryEnvelope = envelope with { AttemptCount = nextAttempt };
            await messagePublisher.PublishAsync(TopicNameResolver.ResolveChannelTopic(envelope.Channel, options.Kafka.Topics), envelope.NotificationId.ToString("N"), retryEnvelope, cancellationToken);
            await notificationRepository.UpdateStatusAsync(envelope.NotificationId, NotificationStatus.Failed, nextAttempt, result.Error, sentAt: null, cancellationToken);
            await PublishAuditAsync(envelope, NotificationStatus.Failed, AuditEventType.RetryScheduled, nextAttempt, result.Error ?? "Transient provider failure.", cancellationToken);
            return;
        }

        await notificationRepository.UpdateStatusAsync(envelope.NotificationId, NotificationStatus.DeadLettered, nextAttempt, result.Error, sentAt: null, cancellationToken);
        await messagePublisher.PublishAsync(TopicNameResolver.ResolveDlqTopic(envelope.Channel, options.Kafka.Topics), envelope.NotificationId.ToString("N"), envelope with { AttemptCount = nextAttempt }, cancellationToken);
        await PublishAuditAsync(envelope, NotificationStatus.DeadLettered, AuditEventType.DeadLettered, nextAttempt, result.Error ?? "Message moved to DLQ.", cancellationToken);
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
