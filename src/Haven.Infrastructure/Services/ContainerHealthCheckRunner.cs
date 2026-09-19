using System.Diagnostics;

using Haven.Application.Common.Interfaces.Services;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.Models;
using Haven.Infrastructure.Deployment.Docker;

namespace Haven.Infrastructure.Services;

public class ContainerHealthCheckRunner(IDockerContainerRuntime containerRuntime) : IHealthCheckRunner
{
    public HealthCheckKind Kind => HealthCheckKind.Container;

    public async Task<HealthCheckRunResult> RunHealthCheckAsync(HealthCheck healthCheck, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var containerResult = await containerRuntime.InspectByServiceIdAsync(healthCheck.ServiceId, cancellationToken);
        if (containerResult.IsFailure)
        {
            return HealthCheckRunResult.Unknown(
                HealthCheckFailureReason.ContainerNotFound,
                "No container exists for this service yet, so there is nothing to check.",
                stopwatch.ElapsedMilliseconds);
        }

        var container = containerResult.Value;
        if (container.State == null)
        {
            return HealthCheckRunResult.Unknown(
                HealthCheckFailureReason.Error,
                "Docker returned no state for the container.",
                stopwatch.ElapsedMilliseconds);
        }

        var duration = stopwatch.ElapsedMilliseconds;

        if (!container.State.Running)
        {
            return HealthCheckRunResult.Unhealthy(
                HealthCheckFailureReason.ContainerNotRunning,
                $"Container is {container.State.Status} (exit code {container.State.ExitCode}).",
                duration,
                exitCode: container.State.ExitCode,
                output: container.State.Error);
        }

        if (container.State.Health is null)
            return HealthCheckRunResult.Healthy("Container is running (the image defines no Docker health check).", duration);

        var lastProbe = container.State.Health.Log?.LastOrDefault();
        var probeOutput = lastProbe?.Output;

        return container.State.Health.Status switch
        {
            "healthy" => HealthCheckRunResult.Healthy("Docker reports the container as healthy.", duration, output: probeOutput),
            "unhealthy" => HealthCheckRunResult.Unhealthy(
                HealthCheckFailureReason.ContainerUnhealthy,
                $"Docker reports the container as unhealthy after {container.State.Health.FailingStreak} failing checks.",
                duration,
                exitCode: lastProbe?.ExitCode,
                output: probeOutput),
            var other => HealthCheckRunResult.Unknown(
                HealthCheckFailureReason.None,
                $"Docker reports the container health as '{other}'.",
                duration,
                probeOutput)
        };
    }
}
