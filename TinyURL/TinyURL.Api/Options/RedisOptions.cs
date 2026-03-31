using System.ComponentModel.DataAnnotations;

namespace TinyURL.Api.Options;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    [Required]
    public string Configuration { get; init; } = "redis:6379";

    public TimeSpan? DefaultTtl { get; init; } = TimeSpan.FromHours(24);
}
