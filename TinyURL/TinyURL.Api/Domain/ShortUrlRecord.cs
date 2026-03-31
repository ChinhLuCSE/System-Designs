namespace TinyURL.Api.Domain;

public sealed record ShortUrlRecord(
    Guid Id,
    string LongUrl,
    string ShortCode,
    DateTimeOffset CreatedAtUtc);
