using System.Text.Json;
using System.Text.Json.Serialization;

namespace NotificationSystem.Shared.Services;

public static class JsonMessageSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize<T>(T payload) => JsonSerializer.Serialize(payload, Options);

    public static T Deserialize<T>(string payload) =>
        JsonSerializer.Deserialize<T>(payload, Options) ?? throw new InvalidOperationException($"Could not deserialize payload to {typeof(T).Name}.");
}
