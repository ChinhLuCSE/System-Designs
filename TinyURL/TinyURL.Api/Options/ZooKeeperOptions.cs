using System.ComponentModel.DataAnnotations;

namespace TinyURL.Api.Options;

public sealed class ZooKeeperOptions
{
    public const string SectionName = "ZooKeeper";

    [Required]
    public string ConnectionString { get; init; } = "zookeeper:2181";

    [Range(1_000, 120_000)]
    public int SessionTimeoutMs { get; init; } = 15_000;

    [Range(1, long.MaxValue)]
    public long InitialValue { get; init; } = 1;

    [Range(1, 10_000_000)]
    public int RangeSize { get; init; } = 1_000_000;

    [Range(1, 20)]
    public int MaxRetries { get; init; } = 10;
}
