using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationSystem.Shared.Abstractions;
using NotificationSystem.Shared.Configuration;
using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Services;

public sealed class PriorityProcessingBackgroundService(
    IOptions<InfrastructureOptions> options,
    INotificationProcessingService processingService,
    ILogger<PriorityProcessingBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = options.Value.Kafka.BootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var highConsumer = CreateConsumer(consumerConfig, options.Value.Kafka.ConsumerGroups.ProcessorHigh, options.Value.Kafka.Topics.HighPriority);
        using var mediumConsumer = CreateConsumer(consumerConfig, options.Value.Kafka.ConsumerGroups.ProcessorMedium, options.Value.Kafka.Topics.MediumPriority);
        using var lowConsumer = CreateConsumer(consumerConfig, options.Value.Kafka.ConsumerGroups.ProcessorLow, options.Value.Kafka.Topics.LowPriority);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await TryConsumeAsync(highConsumer, stoppingToken))
                {
                    continue;
                }

                if (await TryConsumeAsync(mediumConsumer, stoppingToken))
                {
                    continue;
                }

                if (await TryConsumeAsync(lowConsumer, stoppingToken))
                {
                    continue;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(250), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in priority processor.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private static IConsumer<string, string> CreateConsumer(ConsumerConfig config, string groupId, string topic)
    {
        var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig(config) { GroupId = groupId }).Build();
        consumer.Subscribe(topic);
        return consumer;
    }

    private async Task<bool> TryConsumeAsync(IConsumer<string, string> consumer, CancellationToken cancellationToken)
    {
        var result = consumer.Consume(TimeSpan.FromMilliseconds(150));
        if (result is null)
        {
            return false;
        }

        var envelope = JsonMessageSerializer.Deserialize<NotificationEnvelope>(result.Message.Value);
        await processingService.ProcessAsync(envelope, cancellationToken);
        consumer.Commit(result);
        return true;
    }
}
