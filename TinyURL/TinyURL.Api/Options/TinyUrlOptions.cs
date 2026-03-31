using System.ComponentModel.DataAnnotations;

namespace TinyURL.Api.Options;

public sealed class TinyUrlOptions
{
    public const string SectionName = "TinyUrl";

    [Required]
    public string PublicBaseUrl { get; init; } = "http://localhost:8080";

    [Range(1, 10)]
    public int MinimumCodeLength { get; init; } = 7;

    [Range(1, 720)]
    public int CacheTtlHours { get; init; } = 24;
}
