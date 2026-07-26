using System.Text.Json;
using Haven.Application.Common.Contracts.Notifications;
using Haven.Domain.Entities;

namespace Haven.Infrastructure.Notifications;

internal static class NotificationUtils
{
    public static T ToProviderConfig<T>(this NotificationChannelConfig config)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(
                       config.Config,
                       new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? throw new InvalidOperationException("Deserialized config is null.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to deserialize config for channel {config.Id}: {ex.Message}", ex);
        }
    }

    public static DomainEventEnvelope CreateEnvelope(this NotificationAttempt attempt)
    {
        return JsonSerializer.Deserialize<DomainEventEnvelope>(attempt.EventPayload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            ?? throw new InvalidOperationException($"Failed to deserialize event payload for attempt {attempt.Id}");
        
    }
}