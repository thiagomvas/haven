using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Haven.Domain.Events;
using Mediator;

namespace Haven.Application.Features.Manifests.EventHandlers;

public sealed class WriteProjectOnProjectCreatedEventHandler(IManifestSerializer<Project> serializer, IProjectRepository repository) : INotificationHandler<ProjectCreatedEvent>
{
    public async ValueTask Handle(ProjectCreatedEvent notification, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(notification.ProjectId, cancellationToken);
        if (project is not null)
        {
            await serializer.WriteAsync(project, cancellationToken);
        }
    }
}
