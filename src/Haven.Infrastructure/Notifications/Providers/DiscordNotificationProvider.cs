using System.Text;
using System.Text.Json;

using Haven.Application.Common.Contracts.Notifications;
using Haven.Application.Common.Interfaces.Notifications;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Models;
using Haven.Domain;
using Haven.Domain.Entities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Notifications.Providers;

public class DiscordNotificationProvider(
    IHttpClientFactory httpClientFactory,
    IServiceProvider serviceProvider,
    ILogger<DiscordNotificationProvider> logger) : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.Discord;

    public async Task<NotificationProviderResult> SendAsync(NotificationAttempt attempt,
        NotificationChannelConfig config,
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
            return new NotificationProviderResult(false, attempt.EventPayload, null,
                $"Invalid channel config: {ex.Message}");
        }


        var client = httpClientFactory.CreateClient("webhook");

        var payload = await BuildDiscordPayload(attempt.EventPayload, discordConfig.Embed, ct);

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
                return new NotificationProviderResult(true, payload, responseBody, null);
            }

            logger.LogWarning(
                "Webhook delivery failed for attempt {AttemptId} → {StatusCode}: {Body}",
                attempt.Id, (int)response.StatusCode, responseBody);
            return new NotificationProviderResult(
                false, payload, responseBody,
                $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Webhook request failed for attempt {AttemptId}", attempt.Id);
            return new NotificationProviderResult(false, attempt.EventPayload, null, ex.Message);
        }
    }

    private async Task<string> BuildDiscordPayload(string eventPayload, bool embed, CancellationToken ct)
    {
        var envelope = JsonSerializer.Deserialize<DomainEventEnvelope>(eventPayload);
        if (envelope == null)
            throw new InvalidOperationException("Failed to deserialize event payload into envelope.");
        
        var message = JsonSerializer.Serialize(envelope.Message).Trim('"');

        if (!embed) return $"{{ \"content\": \"{message}\" }}";

        var labels = await BuildEmbedLabels(envelope, ct);

        return
            $$"""
              {
                "embeds": [
                  {
                    "title": "{{message}}",
                    "color": {{Random.Shared.Next(0x1000000)}},
                    "fields": [{{(labels.Count > 0 ? string.Join(", ", labels.Select(l => $"{{ \"name\": \"{l.Key}\", \"value\": \"{l.Value}\", \"inline\": true}}")) : "")}}]
                  }
                ]
              }
              """;
    }

    private async Task<Dictionary<string, string>> BuildEmbedLabels(DomainEventEnvelope envelope, CancellationToken ct)
    {
        if (envelope.Data == null) return [];

        var labels = new Dictionary<string, string>();

        if (!envelope.Data.AsObject().TryGetPropertyValue("primaryScope", out var scopeNode) ||
            !envelope.Data.AsObject().TryGetPropertyValue("primaryScopeId", out var scopeIdNode) ||
            scopeNode is null ||
            scopeNode.GetValue<int>() == (int)NotificationScope.Global)
            return labels;

        var scope = (NotificationScope) scopeNode.GetValue<int>();
        if (!Guid.TryParse(scopeIdNode.GetValue<string>(), out var scopeId))
            return labels;


        if (scope is not NotificationScope.Global)
        {
            switch (scope)
            {
                case NotificationScope.Service: await AddServiceLabels(labels, scopeId, ct); break;
                case NotificationScope.Environment: await AddEnvironmentLabels(labels, scopeId, ct); break;
                case NotificationScope.Project: await AddProjectLabels(labels, scopeId, ct); break;
                default: break;
            }
        }

        return labels;
    }

    private async Task AddServiceLabels(Dictionary<string, string> labels, Guid id, CancellationToken ct)
    {
        var serviceRepository = serviceProvider.GetRequiredService<IServiceRepository>();
        var service = await serviceRepository.GetByIdAsync(id, ct);

        if (service == null)
            return;

        labels["Service"] = service.Name;
        labels["Service Id"] = service.Id.ToString();
        if (service.Environment is not null) labels["Environment"] = service.Environment.Name;
        if (service.Environment?.Project is not null) labels["Project"] = service.Environment.Project.Name;
    }

    private async Task AddEnvironmentLabels(Dictionary<string, string> labels, Guid id, CancellationToken ct)
    {
        var environmentRepository = serviceProvider.GetRequiredService<IEnvironmentRepository>();
        var environment = await environmentRepository.GetByIdAsync(id, ct);

        if (environment == null)
            return;

        labels["Environment"] = environment.Name;
        labels["Environment Id"] = environment.Id.ToString();
        if (environment.Project is not null) labels["Project"] = environment.Project.Name;
    }

    private async Task AddProjectLabels(Dictionary<string, string> labels, Guid id, CancellationToken ct)
    {
        var projectRepository = serviceProvider.GetRequiredService<IProjectRepository>();
        var project = await projectRepository.GetByIdAsync(id, ct);

        if (project == null)
            return;

        labels["Project"] = project.Name;
        labels["Project Id"] = project.Id.ToString();
    }
}