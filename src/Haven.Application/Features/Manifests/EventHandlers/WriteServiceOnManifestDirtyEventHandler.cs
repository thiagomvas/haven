using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Events;
using Mediator;

namespace Haven.Application.Features.Manifests.EventHandlers;

public class WriteServiceOnManifestDirtyEventHandler(
    IManifestSerializer serializer,
    IProjectRepository projectRepository,
    IEnvironmentRepository environmentRepository,
    IServiceRepository serviceRepository) : INotificationHandler<ManifestDirtyEvent>
{
    public async ValueTask Handle(ManifestDirtyEvent notification, CancellationToken cancellationToken)
    {
        int pageNumber = 1;
        PagedResult<Haven.Domain.Aggregates.Project> paginated;

        do
        {
            paginated = await projectRepository.GetPagedAsync(pageNumber, 10, cancellationToken);

            foreach (var project in paginated.Items)
            {
                var environments = await environmentRepository.GetByProjectIdAsync(project.Id, cancellationToken);
                foreach (var environment in environments)
                {
                    var services = await serviceRepository.GetByEnvironmentIdAsync(environment.Id, cancellationToken);
                    foreach (var service in services)
                    {
                        await serializer.WriteServiceAsync(project, environment, service, cancellationToken);
                    }
                }
            }

            pageNumber++;
        } while (paginated.HasNextPage);
    }
}
