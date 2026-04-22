using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Infrastructure.Persistence;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Events.Handlers;

public class ContainerUnhealthyEventHandler : INotificationHandler<ContainerUnhealthyEvent>
{
    private readonly HavenDbContext _db;
    private readonly IProjectRepository _repository;
    private readonly ILogger<ContainerUnhealthyEventHandler> _logger;

    public ContainerUnhealthyEventHandler(HavenDbContext db, IProjectRepository repository, ILogger<ContainerUnhealthyEventHandler> logger)
    {
        _db = db;
        _repository = repository;
        _logger = logger;
    }

    public async ValueTask Handle(ContainerUnhealthyEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Container {ContainerId} is unhealthy. Marking service {ServiceId} as degraded",
            notification.ContainerId, notification.ServiceId);

        var project = await _repository.GetByServiceIdAsync(notification.ServiceId, cancellationToken);
        if (project == null)
        {
            _logger.LogWarning("Project not found for service {ServiceId}", notification.ServiceId);
            return;
        }

        var service = project.Environments
            .SelectMany(e => e.Services)
            .FirstOrDefault(s => s.Id == notification.ServiceId);

        if (service == null)
        {
            _logger.LogWarning("Service {ServiceId} not found in project {ProjectId}", notification.ServiceId, project.Id);
            return;
        }

        if (service.Status != ServiceStatus.Running)
        {
            _logger.LogDebug("Service {ServiceId} is not in running state, skipping degraded status update", notification.ServiceId);
            return;
        }

        project.DegradeService(service.EnvironmentId, notification.ServiceId);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Service {ServiceId} marked as degraded due to unhealthy container", notification.ServiceId);
    }
}
