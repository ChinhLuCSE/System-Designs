namespace TinyURL.Api.Contracts;

public sealed record CreateShortUrlResponse(
    Guid Id,
    string LongUrl,
    string ShortCode,
    string ShortUrl,
    DateTimeOffset CreatedAtUtc);
