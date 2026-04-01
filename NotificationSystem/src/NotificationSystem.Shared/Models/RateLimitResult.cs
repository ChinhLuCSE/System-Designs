namespace NotificationSystem.Shared.Models;

public sealed record RateLimitResult(bool Allowed, int CurrentCount, int Limit, DateTimeOffset WindowEndsAt);
