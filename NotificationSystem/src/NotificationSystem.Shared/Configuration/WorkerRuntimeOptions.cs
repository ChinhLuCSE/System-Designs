using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Configuration;

public sealed class WorkerRuntimeOptions
{
    public const string SectionName = "Worker";

    public NotificationChannel Channel { get; init; } = NotificationChannel.Email;

    public string ConsumerGroupId { get; init; } = string.Empty;
}
