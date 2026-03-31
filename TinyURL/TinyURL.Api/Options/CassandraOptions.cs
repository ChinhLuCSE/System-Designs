using System.ComponentModel.DataAnnotations;

namespace TinyURL.Api.Options;

public sealed class CassandraOptions
{
    public const string SectionName = "Cassandra";

    [MinLength(1)]
    public string[] ContactPoints { get; init; } = ["cassandra"];

    [Range(1, 65535)]
    public int Port { get; init; } = 9042;

    [Required]
    public string Keyspace { get; init; } = "tinyurl";

    [Required]
    public string TableName { get; init; } = "urls";

    [Range(1, 20)]
    public int MaxRetries { get; init; } = 10;
}
