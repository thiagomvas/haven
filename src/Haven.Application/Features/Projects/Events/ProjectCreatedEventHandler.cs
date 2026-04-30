using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Application.Features.Projects.Events;

public sealed class ProjectCreatedEventHandler(
    IProjectRepository repository,
    IManifestSerializer serializer,
    ILogger<ProjectCreatedEventHandler> logger) : INotificationHandler<ProjectCreatedEvent>
{
    public async ValueTask Handle(ProjectCreatedEvent notification, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(notification.Id, cancellationToken);
        if (project is null) return;
        await serializer.WriteProjectAsync(project, cancellationToken);
    }
}