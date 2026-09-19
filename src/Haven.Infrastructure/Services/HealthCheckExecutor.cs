using System.Collections.Concurrent;

using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Configuration;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Haven.Infrastructure.Services;

public sealed class HealthCheckExecutor(
    IHealthCheckRepository healthCheckRepository,
    IServiceRepository serviceRepository,
    IHealthCheckRunnerFactory runnerFactory,
    IUnitOfWork unitOfWork,
    IOptionsMonitor<HealthCheckOptions> options,
    ILogger<HealthCheckExecutor> logger) : IHealthCheckExecutor
{
    private const int MaxRetries = 10;

    // Prevents overlapping runs of the same check (slow probe + short cron) from stacking up.
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> RunLocks = new();

    public async Task<HealthCheckRunResult?> ExecuteAsync(Guid healthCheckId, CancellationToken cancellationToken = default)
    {
        var healthCheck = await healthCheckRepository.GetByIdAsync(healthCheckId, cancellationToken);
        if (healthCheck is null)
        {
            logger.LogInformation("Health check '{HealthCheckId}' no longer exists, skipping run", healthCheckId);
            return null;
        }

        var service = await serviceRepository.GetByIdAsync(healthCheck.ServiceId, cancellationToken);
        if (service is null)
        {
            logger.LogWarning("Service '{ServiceId}' not found for health check '{HealthCheckId}'", healthCheck.ServiceId, healthCheckId);
            return null;
        }

        var runLock = RunLocks.GetOrAdd(healthCheckId, _ => new SemaphoreSlim(1, 1));
        if (!await runLock.WaitAsync(TimeSpan.Zero, cancellationToken))
        {
            logger.LogDebug("Health check '{HealthCheckId}' is already running, skipping this run", healthCheckId);
            return HealthCheckRunResult.Unknown(HealthCheckFailureReason.None, "A run of this health check is already in progress.");
        }

        try
        {
            var result = await RunWithRetriesAsync(healthCheck, cancellationToken);
            var previousStatus = healthCheck.LastRunStatus;

            // The service's health is rolled up from all of its enabled checks, but the service is loaded without them.
            // Loading them here lets EF attach them to service.HealthChecks so one check can't mask another's failure.
            await healthCheckRepository.GetForServiceListAsync(service.Id, cancellationToken);

            service.RecordHealthCheckResult(healthCheck, result);
            await healthCheckRepository.AddResultAsync(HealthCheckResult.Create(healthCheck.Id, result, healthCheck.LastRunAt!.Value), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await TrimAsync(healthCheck.Id, cancellationToken);
            Log(healthCheck, service.Name, result, previousStatus);

            return result;
        }
        finally
        {
            runLock.Release();
        }
    }

    public Task<HealthCheckRunResult> TestAsync(HealthCheck healthCheck, CancellationToken cancellationToken = default) =>
        RunOnceAsync(healthCheck, cancellationToken);

    private async Task<HealthCheckRunResult> RunWithRetriesAsync(HealthCheck healthCheck, CancellationToken cancellationToken)
    {
        var maxAttempts = 1 + Math.Clamp(healthCheck.Retries, 0, MaxRetries);
        var delay = TimeSpan.FromSeconds(Math.Max(0, options.CurrentValue.RetryDelaySeconds));

        HealthCheckRunResult result;
        var attempt = 1;
        while (true)
        {
            result = await RunOnceAsync(healthCheck, cancellationToken);

            if (result.Status != ServiceHealth.Unhealthy || attempt >= maxAttempts)
                break;

            logger.LogDebug(
                "Health check '{HealthCheckId}' attempt {Attempt}/{MaxAttempts} failed ({Reason}), retrying",
                healthCheck.Id, attempt, maxAttempts, result.Reason);

            attempt++;
            await Task.Delay(delay, cancellationToken);
        }

        return result with { Attempts = attempt };
    }

    private async Task<HealthCheckRunResult> RunOnceAsync(HealthCheck healthCheck, CancellationToken cancellationToken)
    {
        try
        {
            var runner = runnerFactory.Create(healthCheck.Kind);
            return await runner.RunHealthCheckAsync(healthCheck, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Health check '{HealthCheckId}' ({Kind}) threw while running", healthCheck.Id, healthCheck.Kind);
            return HealthCheckRunResult.Unknown(HealthCheckFailureReason.Error, $"The health check could not be run: {ex.Message}");
        }
    }

    private async Task TrimAsync(Guid healthCheckId, CancellationToken cancellationToken)
    {
        try
        {
            await healthCheckRepository.TrimResultsAsync(healthCheckId, Math.Max(1, options.CurrentValue.ResultRetentionCount), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to trim old results of health check '{HealthCheckId}'", healthCheckId);
        }
    }

    private void Log(HealthCheck healthCheck, string serviceName, HealthCheckRunResult result, ServiceHealth previousStatus)
    {
        if (result.Status != ServiceHealth.Healthy)
        {
            logger.LogWarning(
                "Health check '{HealthCheckName}' ({HealthCheckId}) of service '{ServiceName}' returned {Status}: {Reason} - {Message}",
                healthCheck.Name, healthCheck.Id, serviceName, result.Status, result.Reason, result.Message);
        }
        else if (previousStatus != ServiceHealth.Healthy)
        {
            logger.LogInformation(
                "Health check '{HealthCheckName}' ({HealthCheckId}) of service '{ServiceName}' is healthy again",
                healthCheck.Name, healthCheck.Id, serviceName);
        }
        else
        {
            logger.LogDebug("Health check '{HealthCheckId}' ran with result {Status}", healthCheck.Id, result.Status);
        }
    }
}
