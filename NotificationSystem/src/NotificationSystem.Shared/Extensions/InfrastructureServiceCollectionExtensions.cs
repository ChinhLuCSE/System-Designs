using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NotificationSystem.Shared.Abstractions;
using NotificationSystem.Shared.Configuration;
using NotificationSystem.Shared.Services;
using StackExchange.Redis;

namespace NotificationSystem.Shared.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<InfrastructureOptions>(configuration.GetSection(InfrastructureOptions.SectionName));
        services.Configure<WorkerRuntimeOptions>(configuration.GetSection(WorkerRuntimeOptions.SectionName));

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<InfrastructureOptions>>().Value;
            return ConnectionMultiplexer.Connect(options.Redis.ConnectionString);
        });

        services.AddSingleton<IMessagePublisher, KafkaMessagePublisher>();
        services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();
        services.AddSingleton<IRateLimiter, RedisRateLimiter>();
        services.AddSingleton<IBlueprintCache, RedisBlueprintCache>();
        services.AddSingleton<INotificationProvider, FakeEmailNotificationProvider>();
        services.AddSingleton<INotificationProvider, FakeSmsNotificationProvider>();
        services.AddSingleton<INotificationProvider, FakePushNotificationProvider>();
        services.AddSingleton<INotificationProcessingService, NotificationProcessingService>();
        services.AddSingleton<ChannelDeliveryService>();
        services.AddSingleton<KafkaTopicBootstrapper>();

        return services;
    }
}
