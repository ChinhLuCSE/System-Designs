using Cassandra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NotificationSystem.Data.Persistence;
using NotificationSystem.Shared.Abstractions;
using NotificationSystem.Shared.Configuration;

namespace NotificationSystem.Data.Extensions;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationData(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<MySqlConnectionFactory>();
        services.AddSingleton<INotificationRepository, MySqlNotificationRepository>();
        services.AddSingleton<IAuditRepository, CassandraAuditRepository>();
        services.AddSingleton<ICluster>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<InfrastructureOptions>>().Value;
            return Cluster.Builder()
                .AddContactPoints(options.Cassandra.ContactPoints.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .WithPort(options.Cassandra.Port)
                .WithLoadBalancingPolicy(new DefaultLoadBalancingPolicy(options.Cassandra.Datacenter))
                .Build();
        });

        return services;
    }
}
