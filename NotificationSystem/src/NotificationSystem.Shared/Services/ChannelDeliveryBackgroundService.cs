using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationSystem.Shared.Configuration;
using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Services;

public sealed class ChannelDeliveryBackgroundService(
    IOptions<InfrastructureOptions> infrastructureOptions,
    IOptions<WorkerRuntimeOptions> workerRuntimeOptions,
    ChannelDeliveryService deliveryService,
    ILogger<ChannelDeliveryBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var topics = infrastructureOptions.Value.Kafka.Topics;
        var runtime = workerRuntimeOptions.Value;
        var topic = TopicNameResolver.ResolveChannelTopic(runtime.Channel, topics);
        var consumerGroupId = string.IsNullOrWhiteSpace(runtime.ConsumerGroupId)
            ? $"notification-worker-{runtime.Channel.ToString().ToLowerInvariant()}"
            : runtime.ConsumerGroupId;

        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = infrastructureOptions.Value.Kafka.BootstrapServers,
            GroupId = consumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        }).Build();

        consumer.Subscribe(topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromMilliseconds(250));
                if (result is null)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(150), stoppingToken);
                    continue;
                }

                var envelope = JsonMessageSerializer.Deserialize<NotificationEnvelope>(result.Message.Value);
                await deliveryService.DeliverAsync(envelope, stoppingToken);
                consumer.Commit(result);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in {Channel} worker.", runtime.Channel);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}
