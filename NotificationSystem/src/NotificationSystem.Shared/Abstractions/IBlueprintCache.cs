using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Abstractions;

public interface IBlueprintCache
{
    Task<NotificationBlueprint?> GetAsync(string blueprintId, CancellationToken cancellationToken);

    Task SetAsync(NotificationBlueprint blueprint, CancellationToken cancellationToken);
}
