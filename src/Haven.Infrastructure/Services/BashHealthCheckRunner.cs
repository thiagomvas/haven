using System.Diagnostics;
using System.Net;
using System.Text.Json;

using Docker.DotNet;

using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.HealthChecks;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.Models;
using Haven.Infrastructure.Deployment.Docker;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Services;

public class BashHealthCheckRunner(
    IDockerContainerRuntime containerRuntime,
    ILogger<BashHealthCheckRunner> logger) : IHealthCheckRunner
{
    public HealthCheckKind Kind => HealthCheckKind.Bash;

    public async Task<HealthCheckRunResult> RunHealthCheckAsync(HealthCheck healthCheck, CancellationToken cancellationToken = default)
    {
        BashHealthCheckConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<BashHealthCheckConfig>(healthCheck.Config, HealthCheckConfigValidator.JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid config for health check '{HealthCheckId}'", healthCheck.Id);
            return HealthCheckRunResult.Unknown(HealthCheckFailureReason.InvalidConfig, $"The health check configuration is not valid JSON: {ex.Message}");
        }

        if (config is null || string.IsNullOrWhiteSpace(config.Command))
            return HealthCheckRunResult.Unknown(HealthCheckFailureReason.InvalidConfig, "The health check has no command configured.");

        var timeoutSeconds = Math.Max(1, config.TimeoutSeconds);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var execResult = await containerRuntime.ExecInContainerByServiceIdAsync(
                healthCheck.ServiceId,
                config.Command,
                TimeSpan.FromSeconds(timeoutSeconds),
                cancellationToken);

            if (execResult.IsFailure)
            {
                return HealthCheckRunResult.Unknown(
                    HealthCheckFailureReason.ContainerNotFound,
                    "No container exists for this service yet, so there is nothing to check.",
                    stopwatch.ElapsedMilliseconds);
            }

            var (exitCode, stdOut, stdErr) = execResult.Value;
            var output = string.IsNullOrWhiteSpace(stdErr) ? stdOut : $"{stdOut}\n{stdErr}".Trim();

            return exitCode == config.ExpectedExitCode
                ? HealthCheckRunResult.Healthy($"Command exited with {exitCode}", stopwatch.ElapsedMilliseconds, exitCode: exitCode, output: output)
                : HealthCheckRunResult.Unhealthy(
                    HealthCheckFailureReason.UnexpectedExitCode,
                    $"Command exited with {exitCode}, expected {config.ExpectedExitCode}",
                    stopwatch.ElapsedMilliseconds,
                    exitCode: exitCode,
                    output: output);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckRunResult.Unhealthy(
                HealthCheckFailureReason.Timeout,
                $"Command did not finish within {timeoutSeconds}s",
                stopwatch.ElapsedMilliseconds);
        }
        catch (DockerApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            return HealthCheckRunResult.Unhealthy(
                HealthCheckFailureReason.ContainerNotRunning,
                $"The container is not running: {ex.Message}",
                stopwatch.ElapsedMilliseconds);
        }
    }
}
