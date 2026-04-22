using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Infrastructure.Persistence;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Events.Handlers;

public class ContainerStoppedEventHandler : INotificationHandler<ContainerStoppedEvent>
{
    private readonly HavenDbContext _db;
    private readonly IProjectRepository _repository;
    private readonly ILogger<ContainerStoppedEventHandler> _logger;

    public ContainerStoppedEventHandler(HavenDbContext db, IProjectRepository repository, ILogger<ContainerStoppedEventHandler> logger)
    {
        _db = db;
        _repository = repository;
        _logger = logger;
    }

    public async ValueTask Handle(ContainerStoppedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Container {ContainerId} stopped. Scheduling deployment check for service {ServiceId}",
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

        if (service.Status == ServiceStatus.Stopped)
        {
            _logger.LogInformation("Service {ServiceId} is already stopped, skipping stop handler", notification.ServiceId);
            return;
        }

        project.StopService(service.EnvironmentId, service.Id);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Service {ServiceId} marked as stopped", notification.ServiceId);
    }
}
