using TinyURL.Api.Abstractions;

namespace TinyURL.Api.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static async Task InitializeInfrastructureAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUrlRepository>();
        var rangeAllocator = scope.ServiceProvider.GetRequiredService<IRangeAllocator>();

        await repository.InitializeAsync(CancellationToken.None);
        await rangeAllocator.WarmupAsync(CancellationToken.None);
    }
}
