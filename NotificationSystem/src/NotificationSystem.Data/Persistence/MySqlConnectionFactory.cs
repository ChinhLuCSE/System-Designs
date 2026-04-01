using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using NotificationSystem.Shared.Configuration;

namespace NotificationSystem.Data.Persistence;

public sealed class MySqlConnectionFactory(IOptions<InfrastructureOptions> options, ILogger<MySqlConnectionFactory> logger)
{
    public async Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 15;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var connection = new MySqlConnection(options.Value.MySql.ConnectionString);
                await connection.OpenAsync(cancellationToken);
                return connection;
            }
            catch (MySqlException ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "MySQL connection attempt {Attempt}/{MaxAttempts} failed. Retrying.", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        throw new InvalidOperationException("MySQL connection retry loop exited unexpectedly.");
    }
}
