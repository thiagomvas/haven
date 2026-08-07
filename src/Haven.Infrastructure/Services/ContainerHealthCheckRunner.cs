using Docker.DotNet;

using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Deployment;
using Haven.Infrastructure.Deployment.Docker;

namespace Haven.Infrastructure.Services;

public class ContainerHealthCheckRunner(IDockerContainerRuntime containerRuntime, IServiceRepository serviceRepository) : IHealthCheckRunner
{
    public HealthCheckKind Kind => HealthCheckKind.Container;
    public async Task<ServiceHealth> RunHealthCheckAsync(HealthCheck healthCheck, CancellationToken cancellationToken = default)
    {
        var service = healthCheck.Service ?? await serviceRepository.GetByIdAsync(healthCheck.ServiceId, cancellationToken);
        if (service == null)
            throw new InvalidOperationException($"Service with ID {healthCheck.ServiceId} not found for health check {healthCheck.Name}.");

        var containerResult = await containerRuntime.InspectByServiceIdAsync(service.Id, cancellationToken);
        if (containerResult.IsFailure)
        {
            return ServiceHealth.Unknown;
        }

        var container = containerResult.Value;
        if (container.State == null)
        {
            return ServiceHealth.Unknown;
        }

        if (container.State.Health != null)
        {
            return container.State.Health.Status switch
            {
                "healthy" => ServiceHealth.Healthy,
                "unhealthy" => ServiceHealth.Unhealthy,
                _ => ServiceHealth.Unknown
            };
        }

        return container.State.Running ? ServiceHealth.Healthy : ServiceHealth.Unhealthy;
    }
}