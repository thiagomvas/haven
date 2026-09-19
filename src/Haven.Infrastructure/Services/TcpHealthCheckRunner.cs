using System.Text.Json;

using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.HealthChecks;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.Models;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Services;

public class TcpHealthCheckRunner : IHealthCheckRunner
{
    private readonly IHealthCheckProbe _probe;
    private readonly IHealthCheckTargetResolver _targetResolver;
    private readonly ILogger<TcpHealthCheckRunner> _logger;

    public TcpHealthCheckRunner(IHealthCheckProbe probe, IHealthCheckTargetResolver targetResolver, ILogger<TcpHealthCheckRunner> logger)
    {
        _probe = probe;
        _targetResolver = targetResolver;
        _logger = logger;
    }

    public HealthCheckKind Kind => HealthCheckKind.Tcp;

    public async Task<HealthCheckRunResult> RunHealthCheckAsync(HealthCheck healthCheck, CancellationToken cancellationToken = default)
    {
        TcpHealthCheckConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<TcpHealthCheckConfig>(healthCheck.Config, HealthCheckConfigValidator.JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid config for health check '{HealthCheckId}'", healthCheck.Id);
            return HealthCheckRunResult.Unknown(HealthCheckFailureReason.InvalidConfig, $"The health check configuration is not valid JSON: {ex.Message}");
        }

        if (config is null || string.IsNullOrWhiteSpace(config.Host) || config.Port is < 1 or > 65535)
            return HealthCheckRunResult.Unknown(HealthCheckFailureReason.InvalidConfig, "The health check needs a host and a port between 1 and 65535.");

        var target = await _targetResolver.ResolveAsync(healthCheck.ServiceId, cancellationToken);
        if (target.IsFailure)
            return HealthCheckRunResult.Unknown(HealthCheckFailureReason.ContainerNotFound, "No container exists for this service yet, so there is nothing to check.");

        if (!target.Value.IsRunning)
            return HealthCheckRunResult.Unhealthy(HealthCheckFailureReason.ContainerNotRunning, $"Container '{target.Value.ContainerName}' is not running.");

        if (target.Value.NetworkName is null)
            return HealthCheckRunResult.Unknown(HealthCheckFailureReason.ProbeUnavailable, $"Container '{target.Value.ContainerName}' is not attached to a network the probe can join.");

        var host = HealthCheckPlaceholders.Apply(config.Host, target.Value, out var placeholderError);
        if (host is null)
            return HealthCheckRunResult.Unknown(HealthCheckFailureReason.InvalidConfig, placeholderError!);

        var timeoutSeconds = Math.Max(1, config.TimeoutSeconds);
        var output = await _probe.RunAsync(
            new ProbeRequest(target.Value.NetworkName, ProbeCommands.TcpEntrypoint, ProbeCommands.BuildTcpArgs(host, config.Port, timeoutSeconds), TimeSpan.FromSeconds(timeoutSeconds)),
            cancellationToken);

        if (output.IsFailure)
            return HealthCheckRunResult.Unknown(HealthCheckFailureReason.ProbeUnavailable, output.Error.Message);

        return ProbeCommands.ParseTcp(output.Value, host, config.Port, timeoutSeconds);
    }
}
