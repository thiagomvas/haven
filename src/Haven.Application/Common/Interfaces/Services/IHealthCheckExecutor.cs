using Haven.Domain.Entities;
using Haven.Domain.Models;

namespace Haven.Application.Common.Interfaces.Services;

public interface IHealthCheckExecutor
{
    /// <summary>
    /// Runs a stored health check (with its configured retries), records the result and rolls the outcome up
    /// into the service's health. Returns null when the health check or its service no longer exists.
    /// </summary>
    Task<HealthCheckRunResult?> ExecuteAsync(Guid healthCheckId, CancellationToken cancellationToken = default);

    /// <summary>Runs a transient health check once without persisting anything.</summary>
    Task<HealthCheckRunResult> TestAsync(HealthCheck healthCheck, CancellationToken cancellationToken = default);
}
