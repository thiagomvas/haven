using System.Text;
using System.Text.Json;

using Haven.Application.Common.Contracts.Notifications;
using Haven.Application.Common.Interfaces.Notifications;
using Haven.Application.Common.Models;
using Haven.Domain;
using Haven.Domain.Entities;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Notifications.Providers;

public class DiscordNotificationProvider(
    IHttpClientFactory httpClientFactory,
    ILogger<DiscordNotificationProvider> logger) : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.Discord;

    public async Task<NotificationProviderResult> SendAsync(NotificationAttempt attempt, NotificationChannelConfig config,
        CancellationToken ct = default)
    {
        
        DiscordNotificationConfig discordConfig;
        try
        {
            discordConfig = JsonSerializer.Deserialize<DiscordNotificationConfig>(
                                config.Config,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                            ?? throw new InvalidOperationException("Deserialized webhook config is null.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize webhook config for channel {ChannelId}", config.Id);
            return new NotificationProviderResult(false, attempt.EventPayload, null, $"Invalid channel config: {ex.Message}");
        }
        
        
        var client = httpClientFactory.CreateClient("webhook");
        
        var payload = BuildDiscordPayload(attempt.EventPayload, discordConfig.Embed);

        using var request = new HttpRequestMessage(HttpMethod.Post, discordConfig.WebhookUrl);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        try
        {
            using var response = await client.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Webhook delivered for attempt {AttemptId} → {StatusCode}",
                    attempt.Id, (int)response.StatusCode);
                return new NotificationProviderResult(true, attempt.EventPayload, responseBody, null);
            }

            logger.LogWarning(
                "Webhook delivery failed for attempt {AttemptId} → {StatusCode}: {Body}",
                attempt.Id, (int)response.StatusCode, responseBody);
            return new NotificationProviderResult(
                false, attempt.EventPayload, responseBody,
                $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Webhook request failed for attempt {AttemptId}", attempt.Id);
            return new NotificationProviderResult(false, attempt.EventPayload, null, ex.Message);
        }
    }
    
    private string BuildDiscordPayload(string eventPayload, bool embed)
    {
        var envelope = JsonSerializer.Deserialize<DomainEventEnvelope>(eventPayload);
        if (envelope == null)
            throw new InvalidOperationException("Failed to deserialize event payload into envelope.");

        if (!embed) return $"{{ \"content\": \"{envelope.Message}\" }}";
        return $"{{ \"content\": \"EMBED {envelope.Message}\" }}";
    }
}