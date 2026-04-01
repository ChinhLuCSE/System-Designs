namespace NotificationSystem.Shared.Abstractions;

public interface IMessagePublisher
{
    Task PublishAsync<T>(string topic, string key, T payload, CancellationToken cancellationToken);
}
