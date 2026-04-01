namespace NotificationSystem.Shared.Models;

public sealed record ProviderDeliveryResult(bool Succeeded, bool IsTransientFailure, string? ProviderReference, string? Error);
