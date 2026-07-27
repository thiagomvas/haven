using Haven.Domain;
using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Services;

public interface IHealthCheckRunner
{
    HealthCheckKind Kind { get; }
    Task<ServiceStatus> RunHealthCheckAsync(HealthCheck healthCheck, CancellationToken cancellationToken = default);
}