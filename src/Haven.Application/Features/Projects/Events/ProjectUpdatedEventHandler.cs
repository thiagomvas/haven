using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Haven.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;


namespace Haven.Application.Features.Projects.Events;

public sealed class ProjectUpdatedEventHandler(
    IProjectRepository repository,
    IManifestSerializer serializer) : INotificationHandler<ProjectUpdatedEvent>
{
    public async ValueTask Handle(ProjectUpdatedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.OldName == notification.NewName)
        {
            return;
        }
        var project = await repository.GetByIdAsync(notification.ProjectId, cancellationToken);
        if (project is null) return;
        
        if (!string.IsNullOrWhiteSpace(notification.OldName) && !string.IsNullOrWhiteSpace(notification.NewName))
            await serializer.RenameProjectAsync(notification.OldName, notification.NewName, cancellationToken);
        
        await serializer.WriteProjectAsync(project, cancellationToken);
    }
}
