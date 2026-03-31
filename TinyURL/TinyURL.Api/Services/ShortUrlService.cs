using Microsoft.Extensions.Options;
using TinyURL.Api.Abstractions;
using TinyURL.Api.Contracts;
using TinyURL.Api.Domain;
using TinyURL.Api.Options;

namespace TinyURL.Api.Services;

public sealed class ShortUrlService(
    IRangeAllocator rangeAllocator,
    Base62Encoder base62Encoder,
    IUrlRepository repository,
    IUrlCache cache,
    IOptions<TinyUrlOptions> options) : IShortUrlService
{
    private readonly TinyUrlOptions _options = options.Value;

    public async Task<CreateShortUrlResponse> CreateAsync(string longUrl, CancellationToken cancellationToken)
    {
        var nextId = await rangeAllocator.GetNextIdAsync(cancellationToken);
        var shortCode = base62Encoder.Encode(nextId, _options.MinimumCodeLength);
        var record = new ShortUrlRecord(
            Guid.CreateVersion7(),
            longUrl,
            shortCode,
            DateTimeOffset.UtcNow);

        await repository.SaveAsync(record, cancellationToken);
        await cache.SetAsync(shortCode, longUrl, cancellationToken);

        return new CreateShortUrlResponse(
            record.Id,
            record.LongUrl,
            record.ShortCode,
            $"{_options.PublicBaseUrl.TrimEnd('/')}/{record.ShortCode}",
            record.CreatedAtUtc);
    }

    public async Task<string?> ResolveAsync(string shortCode, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync(shortCode, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            return cached;
        }

        var record = await repository.GetByShortCodeAsync(shortCode, cancellationToken);
        if (record is null)
        {
            return null;
        }

        await cache.SetAsync(shortCode, record.LongUrl, cancellationToken);
        return record.LongUrl;
    }
}
