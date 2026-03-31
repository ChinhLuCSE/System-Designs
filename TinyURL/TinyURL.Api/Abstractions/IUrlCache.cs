namespace TinyURL.Api.Abstractions;

public interface IUrlCache
{
    Task<string?> GetAsync(string shortCode, CancellationToken cancellationToken);
    Task SetAsync(string shortCode, string longUrl, CancellationToken cancellationToken);
}
