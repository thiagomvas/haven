using Haven.Application.Common.Interfaces.Notifications;
using Haven.Application.Common.Models;
using Haven.Application.Configuration;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Events;
using Haven.Infrastructure.Notifications.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Haven.Infrastructure.Notifications.Providers;

public class NtfyNotificationProvider(HttpClient httpClient, ILogger<NtfyNotificationProvider> logger)
    : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.Ntfy;

    private const string MaxPriority = "max";
    private const string HighPriority = "high";
    private const string DefaultPriority = "default";
    private const string LowPriority = "low";
    private const string MinPriority = "min";

    public async Task<NotificationProviderResult> SendAsync(NotificationAttempt attempt,
        NotificationChannelConfig config, CancellationToken ct = default)
    {
        var ntfyConfig = config.ToProviderConfig<NtfyNotificationConfig>();
        var envelope = attempt.CreateEnvelope();

        var message = envelope.Message;
        var url = ntfyConfig.ToUrl();

        var headers = new Dictionary<string, string>
        {
            { "Title", envelope.ToFormattedEventName() },
            { "Priority", EventTypeToPriority(envelope.EventType) },
            { "Tags", envelope.EventType }
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        requestMessage.Content = new StringContent(message);

        foreach (var header in headers)
            requestMessage.Headers.Add(header.Key, header.Value);

        using var response = httpClient.SendAsync(requestMessage, ct).Result;
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Notification sent successfully via Ntfy: {Message}", message);
            return new NotificationProviderResult(true, message, responseBody, null);
        }

        logger.LogWarning("Failed to send notification via Ntfy. Status Code: {StatusCode}, Response: {ResponseBody}",
            response.StatusCode, responseBody);
        return new NotificationProviderResult(false, message, responseBody,
            $"Failed to send notification: {response.StatusCode}");
    }

    private static readonly Dictionary<string, string> EventTypePriorityMap = new()
    {
        { nameof(ServiceStoppedEvent), MaxPriority },
    };

    private static string EventTypeToPriority(string eventType)
    {
        if (EventTypePriorityMap.TryGetValue(eventType, out var priority))
            return priority;

        if (eventType.Contains("Updated") || eventType.Contains("Created"))
            return LowPriority;
        if (eventType.Contains("Deleted"))
            return HighPriority;

        return DefaultPriority;
    }
}