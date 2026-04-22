using Haven.Application.Common.Interfaces.Repositories;
using Haven.Infrastructure.Persistence;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Events.Handlers;

public class ContainerKilledEventHandler : INotificationHandler<ContainerKilledEvent>
{
    private readonly HavenDbContext _db;
    private readonly IProjectRepository _repository;
    private readonly ILogger<ContainerKilledEventHandler> _logger;

    public ContainerKilledEventHandler(HavenDbContext db, IProjectRepository repository, ILogger<ContainerKilledEventHandler> logger)
    {
        _db = db;
        _repository = repository;
        _logger = logger;
    }

    public async ValueTask Handle(ContainerKilledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Container {ContainerId} was killed. Marking service {ServiceId} as stopped",
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

        project.StopService(service.EnvironmentId, service.Id);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Service {ServiceId} marked as stopped due to container kill", notification.ServiceId);
    }
}
