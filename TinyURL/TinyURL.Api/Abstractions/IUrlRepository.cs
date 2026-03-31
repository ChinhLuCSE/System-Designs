using TinyURL.Api.Domain;

namespace TinyURL.Api.Abstractions;

public interface IUrlRepository
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task<ShortUrlRecord?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken);
    Task SaveAsync(ShortUrlRecord record, CancellationToken cancellationToken);
}
