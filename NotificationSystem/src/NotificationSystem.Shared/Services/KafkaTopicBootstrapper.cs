using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationSystem.Shared.Configuration;

namespace NotificationSystem.Shared.Services;

public sealed class KafkaTopicBootstrapper(IOptions<InfrastructureOptions> options, ILogger<KafkaTopicBootstrapper> logger)
{
    public async Task EnsureTopicsAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 15;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = options.Value.Kafka.BootstrapServers,
                ClientId = options.Value.Kafka.ClientId
            }).Build();

            var topics = TopicNameResolver.AllTopics(options.Value.Kafka.Topics)
                .Select(topic => new TopicSpecification
                {
                    Name = topic,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                })
                .ToList();

            try
            {
                await adminClient.CreateTopicsAsync(topics).WaitAsync(cancellationToken);
                return;
            }
            catch (CreateTopicsException ex) when (ex.Results.All(result => result.Error.Code == ErrorCode.TopicAlreadyExists))
            {
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Kafka topic bootstrap attempt {Attempt}/{MaxAttempts} failed. Retrying.", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        throw new InvalidOperationException("Kafka topic bootstrap retry loop exited unexpectedly.");
    }
}
