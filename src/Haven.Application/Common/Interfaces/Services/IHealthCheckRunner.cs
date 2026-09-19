using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.Models;

namespace Haven.Application.Common.Interfaces.Services;

public interface IHealthCheckRunner
{
    HealthCheckKind Kind { get; }

    /// <summary>Runs the check once. Implementations must not throw for check failures; they report them in the result.</summary>
    Task<HealthCheckRunResult> RunHealthCheckAsync(HealthCheck healthCheck, CancellationToken cancellationToken = default);
}
