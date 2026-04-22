using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Infrastructure.Persistence;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Events.Handlers;

public class ContainerStartedEventHandler : INotificationHandler<ContainerStartedEvent>
{
    private readonly HavenDbContext _db;
    private readonly IProjectRepository _repository;
    private readonly ILogger<ContainerStartedEventHandler> _logger;

    public ContainerStartedEventHandler(HavenDbContext db, IProjectRepository repository, ILogger<ContainerStartedEventHandler> logger)
    {
        _db = db;
        _repository = repository;
        _logger = logger;
    }

    public async ValueTask Handle(ContainerStartedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Container {ContainerId} started. Marking service {ServiceId} as running",
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

        project.DeployService(service.EnvironmentId, service.Id);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Service {ServiceId} marked as running after container started", notification.ServiceId);
    }
}
