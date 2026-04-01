using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Abstractions;

public interface IRateLimiter
{
    Task<RateLimitResult> EvaluateAsync(NotificationEnvelope envelope, CancellationToken cancellationToken);
}
