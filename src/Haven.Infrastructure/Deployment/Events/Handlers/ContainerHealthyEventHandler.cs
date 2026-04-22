using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Infrastructure.Persistence;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Events.Handlers;

public class ContainerHealthyEventHandler : INotificationHandler<ContainerHealthyEvent>
{
    private readonly HavenDbContext _db;
    private readonly IProjectRepository _repository;
    private readonly ILogger<ContainerHealthyEventHandler> _logger;

    public ContainerHealthyEventHandler(HavenDbContext db, IProjectRepository repository, ILogger<ContainerHealthyEventHandler> logger)
    {
        _db = db;
        _repository = repository;
        _logger = logger;
    }

    public async ValueTask Handle(ContainerHealthyEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Container {ContainerId} is now healthy. Marking service {ServiceId} as running",
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

        if (service.Status == ServiceStatus.Running)
        {
            _logger.LogDebug("Service {ServiceId} is already running", notification.ServiceId);
            return;
        }

        if (service.Status == ServiceStatus.Degraded)
        {
            project.DeployService(service.EnvironmentId, service.Id);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Service {ServiceId} recovered from degraded state", notification.ServiceId);
        }
    }
}
