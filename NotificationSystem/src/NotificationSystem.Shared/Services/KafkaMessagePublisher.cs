using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationSystem.Shared.Abstractions;
using NotificationSystem.Shared.Configuration;

namespace NotificationSystem.Shared.Services;

public sealed class KafkaMessagePublisher : IMessagePublisher, IDisposable
{
    private readonly IProducer<string, string> producer;
    private readonly ILogger<KafkaMessagePublisher> logger;

    public KafkaMessagePublisher(IOptions<InfrastructureOptions> options, ILogger<KafkaMessagePublisher> logger)
    {
        this.logger = logger;
        producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = options.Value.Kafka.BootstrapServers,
            ClientId = options.Value.Kafka.ClientId,
            EnableIdempotence = true,
            Acks = Acks.All
        }).Build();
    }

    public async Task PublishAsync<T>(string topic, string key, T payload, CancellationToken cancellationToken)
    {
        var result = await producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = key,
            Value = JsonMessageSerializer.Serialize(payload)
        }, cancellationToken);

        logger.LogInformation("Published message to {TopicPartitionOffset}.", result.TopicPartitionOffset);
    }

    public void Dispose()
    {
        producer.Flush(TimeSpan.FromSeconds(5));
        producer.Dispose();
    }
}
