using Docker.DotNet;

using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Infrastructure.Deployment;

namespace Haven.Infrastructure.Services;

public class ContainerHealthCheckRunner(IDockerContainerRuntime containerRuntime, IServiceRepository serviceRepository) : IHealthCheckRunner
{
    public HealthCheckKind Kind => HealthCheckKind.Container;
    public async Task<ServiceStatus> RunHealthCheckAsync(HealthCheck healthCheck, CancellationToken cancellationToken = default)
    {
        var service = healthCheck.Service ?? await serviceRepository.GetByIdAsync(healthCheck.ServiceId, cancellationToken);
        if (service == null)
            throw new InvalidOperationException($"Service with ID {healthCheck.ServiceId} not found for health check {healthCheck.Name}.");
        
        var containerResult = await containerRuntime.InspectByServiceIdAsync(service.Id, cancellationToken);
        if (containerResult.IsFailure)
        {
            return ServiceStatus.Unknown;
        }
        
        var container = containerResult.Value;
        if (container.State == null)
        {
            return ServiceStatus.Unknown;
        }
        
        if (container.State.Health != null)
        {
            return container.State.Health.Status switch
            {
                "healthy" => ServiceStatus.Running,
                "unhealthy" => ServiceStatus.Degraded,
                _ => ServiceStatus.Unknown
            };
        }
        
        return ServiceStatus.Unknown;
    }
}