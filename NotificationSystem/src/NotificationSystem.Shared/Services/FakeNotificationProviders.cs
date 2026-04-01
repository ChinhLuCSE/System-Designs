using NotificationSystem.Shared.Abstractions;
using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Services;

internal static class FakeProviderBehavior
{
    public static ProviderDeliveryResult Evaluate(NotificationEnvelope envelope, string destination, string providerName)
    {
        if (destination.Contains("permanent-fail", StringComparison.OrdinalIgnoreCase) ||
            envelope.Metadata.TryGetValue("mode", out var mode) && mode.Equals("permanent-fail", StringComparison.OrdinalIgnoreCase))
        {
            return new ProviderDeliveryResult(false, false, null, $"{providerName} simulated permanent failure.");
        }

        if (destination.Contains("transient-fail", StringComparison.OrdinalIgnoreCase) ||
            envelope.Metadata.TryGetValue("mode", out var transientMode) && transientMode.Equals("transient-fail", StringComparison.OrdinalIgnoreCase))
        {
            if (envelope.AttemptCount < 2)
            {
                return new ProviderDeliveryResult(false, true, null, $"{providerName} simulated transient failure.");
            }
        }

        return new ProviderDeliveryResult(true, false, $"{providerName}-{envelope.NotificationId:N}", null);
    }
}

public sealed class FakeEmailNotificationProvider : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public Task<ProviderDeliveryResult> DeliverAsync(NotificationEnvelope envelope, DeviceSetting destination, NotificationBlueprint? blueprint, CancellationToken cancellationToken)
        => Task.FromResult(FakeProviderBehavior.Evaluate(envelope, destination.Destination, "email"));
}

public sealed class FakeSmsNotificationProvider : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.Sms;

    public Task<ProviderDeliveryResult> DeliverAsync(NotificationEnvelope envelope, DeviceSetting destination, NotificationBlueprint? blueprint, CancellationToken cancellationToken)
        => Task.FromResult(FakeProviderBehavior.Evaluate(envelope, destination.Destination, "sms"));
}

public sealed class FakePushNotificationProvider : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.Push;

    public Task<ProviderDeliveryResult> DeliverAsync(NotificationEnvelope envelope, DeviceSetting destination, NotificationBlueprint? blueprint, CancellationToken cancellationToken)
        => Task.FromResult(FakeProviderBehavior.Evaluate(envelope, destination.DeviceToken ?? destination.Destination, "push"));
}
