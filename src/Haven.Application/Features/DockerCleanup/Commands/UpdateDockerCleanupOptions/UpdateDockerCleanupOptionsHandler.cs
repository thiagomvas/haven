using System.Text.Json;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.Configuration.Events;

namespace Haven.Application.Features.DockerCleanup.Commands.UpdateDockerCleanupOptions;

public sealed class UpdateDockerCleanupOptionsHandler(
    IHavenSettingRepository repository,
    IHavenConfigurationStore store,
    IUnitOfWork unitOfWork,
    Mediator.IMediator mediator)
    : ICommandHandler<UpdateDockerCleanupOptionsCommand, DockerCleanupOptions>
{
    public async ValueTask<Result<DockerCleanupOptions>> Handle(UpdateDockerCleanupOptionsCommand request, CancellationToken ct)
    {
        await repository.UpsertAsync(DockerCleanupOptions.SectionName, JsonSerializer.Serialize(request.Options), ct);
        unitOfWork.OnAfterSave(() => store.Invalidate(DockerCleanupOptions.SectionName));

        await mediator.Publish(new ConfigurationUpdatedNotification(), ct);

        return Result<DockerCleanupOptions>.Success(request.Options);
    }
}
