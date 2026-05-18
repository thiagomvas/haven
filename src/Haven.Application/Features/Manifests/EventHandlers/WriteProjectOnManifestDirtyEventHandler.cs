using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;
using Haven.Domain.Events;
using Mediator;

namespace Haven.Application.Features.Manifests.EventHandlers;

public class WriteProjectOnManifestDirtyEventHandler(IManifestSerializer<Project> serializer, IProjectRepository repository) : INotificationHandler<ManifestDirtyEvent>
{
    public async ValueTask Handle(ManifestDirtyEvent notification, CancellationToken cancellationToken)
    {
        int pageNumber = 1;
        PagedResult<Project> paginated;

        do
        {
            paginated = await repository.GetPagedAsync(pageNumber, 10, cancellationToken);

            foreach (var project in paginated.Items)
            {
                await serializer.WriteAsync(project, cancellationToken);
            }

            pageNumber++;
        } while (paginated.HasNextPage);
    }
}