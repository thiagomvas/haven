using System.Text.Json;

using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.HealthChecks;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Deployment;
using Haven.Infrastructure.Deployment.Docker;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Services;

public class BashHealthCheckRunner(
    IDockerContainerRuntime containerRuntime,
    IServiceRepository serviceRepository,
    ILogger<BashHealthCheckRunner> logger) : IHealthCheckRunner
{
    public HealthCheckKind Kind => HealthCheckKind.Bash;

    public async Task<ServiceHealth> RunHealthCheckAsync(HealthCheck healthCheck, CancellationToken cancellationToken = default)
    {
        BashHealthCheckConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<BashHealthCheckConfig>(healthCheck.Config, HealthCheckConfigValidator.JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid config for health check '{HealthCheckId}'", healthCheck.Id);
            return ServiceHealth.Unknown;
        }

        if (config is null || string.IsNullOrWhiteSpace(config.Command))
            return ServiceHealth.Unknown;

        var service = healthCheck.Service ?? await serviceRepository.GetByIdAsync(healthCheck.ServiceId, cancellationToken);
        if (service is null)
            throw new InvalidOperationException($"Service with ID {healthCheck.ServiceId} not found for health check {healthCheck.Name}.");

        try
        {
            var execResult = await containerRuntime.ExecInContainerByServiceIdAsync(
                service.Id,
                config.Command,
                TimeSpan.FromSeconds(Math.Max(1, config.TimeoutSeconds)),
                cancellationToken);

            if (execResult.IsFailure)
                return ServiceHealth.Unknown;

            return execResult.Value.ExitCode == config.ExpectedExitCode
                ? ServiceHealth.Healthy
                : ServiceHealth.Unhealthy;
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            logger.LogDebug(ex, "Bash health check '{HealthCheckId}' timed out", healthCheck.Id);
            return ServiceHealth.Unhealthy;
        }
    }
}