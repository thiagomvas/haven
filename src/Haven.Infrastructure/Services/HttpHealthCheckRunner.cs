using System.Text.Json;

using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.HealthChecks;
using Haven.Domain;
using Haven.Domain.Entities;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Services;

public class HttpHealthCheckRunner(IHttpClientFactory httpClientFactory, ILogger<HttpHealthCheckRunner> logger) : IHealthCheckRunner
{
    public HealthCheckKind Kind => HealthCheckKind.Http;

    public async Task<ServiceHealth> RunHealthCheckAsync(HealthCheck healthCheck, CancellationToken cancellationToken = default)
    {
        HttpHealthCheckConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<HttpHealthCheckConfig>(healthCheck.Config, HealthCheckConfigValidator.JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid config for health check '{HealthCheckId}'", healthCheck.Id);
            return ServiceHealth.Unknown;
        }

        if (config is null || string.IsNullOrWhiteSpace(config.Url))
            return ServiceHealth.Unknown;

        var client = httpClientFactory.CreateClient(nameof(HttpHealthCheckRunner));
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, config.TimeoutSeconds)));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            using var request = new HttpRequestMessage(new HttpMethod(config.Method), config.Url);
            using var response = await client.SendAsync(request, linkedCts.Token);

            var expectedCodes = config.ExpectedStatusCodes is { Length: > 0 } ? config.ExpectedStatusCodes : [200];
            return expectedCodes.Contains((int)response.StatusCode)
                ? ServiceHealth.Healthy
                : ServiceHealth.Unhealthy;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            logger.LogDebug(ex, "Http health check '{HealthCheckId}' failed to reach '{Url}'", healthCheck.Id, config.Url);
            return ServiceHealth.Unhealthy;
        }
    }
}