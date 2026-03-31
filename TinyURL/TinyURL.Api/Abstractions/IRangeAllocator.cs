namespace TinyURL.Api.Abstractions;

public interface IRangeAllocator
{
    Task<long> GetNextIdAsync(CancellationToken cancellationToken);
    Task WarmupAsync(CancellationToken cancellationToken);
}
