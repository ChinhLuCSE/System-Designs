using TinyURL.Api.Contracts;

namespace TinyURL.Api.Abstractions;

public interface IShortUrlService
{
    Task<CreateShortUrlResponse> CreateAsync(string longUrl, CancellationToken cancellationToken);
    Task<string?> ResolveAsync(string shortCode, CancellationToken cancellationToken);
}
