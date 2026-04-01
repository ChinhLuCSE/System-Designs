using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationSystem.Shared.Abstractions;
using NotificationSystem.Shared.Configuration;
using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Services;

public sealed class AuditLoggingBackgroundService(
    IOptions<InfrastructureOptions> options,
    IAuditRepository auditRepository,
    ILogger<AuditLoggingBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = options.Value.Kafka.BootstrapServers,
            GroupId = options.Value.Kafka.ConsumerGroups.AuditLogger,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        }).Build();

        consumer.Subscribe(options.Value.Kafka.Topics.Audit);

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

                var auditEvent = JsonMessageSerializer.Deserialize<AuditEvent>(result.Message.Value);
                await auditRepository.AppendAsync(auditEvent, stoppingToken);
                consumer.Commit(result);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in audit logger.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}
