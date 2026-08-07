using System.Text;
using System.Text.Json;

using Haven.Application.Common.Contracts.Notifications;
using Haven.Application.Common.Interfaces.Notifications;
using Haven.Application.Common.Models;
using Haven.Domain;
using Haven.Domain.Entities;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Notifications.Providers;

public sealed class WebhookNotificationProvider(
    IHttpClientFactory httpClientFactory,
    ILogger<WebhookNotificationProvider> logger)
    : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.Webhook;

    public async Task<NotificationProviderResult> SendAsync(
        NotificationAttempt attempt,
        NotificationChannelConfig config,
        CancellationToken ct = default)
    {
        WebhookNotificationConfig webhookConfig;
        try
        {
            webhookConfig = JsonSerializer.Deserialize<WebhookNotificationConfig>(
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

        using var request = new HttpRequestMessage(HttpMethod.Post, webhookConfig.Url);
        request.Content = new StringContent(attempt.EventPayload, Encoding.UTF8, "application/json");

        foreach (var (key, value) in webhookConfig.Headers)
            request.Headers.TryAddWithoutValidation(key, value);

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
}