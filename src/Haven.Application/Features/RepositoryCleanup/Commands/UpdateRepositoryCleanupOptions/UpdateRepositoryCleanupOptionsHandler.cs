using System.Text.Json;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.Configuration.Events;

namespace Haven.Application.Features.RepositoryCleanup.Commands.UpdateRepositoryCleanupOptions;

public sealed class UpdateRepositoryCleanupOptionsHandler(
    IHavenSettingRepository repository,
    IHavenConfigurationStore store,
    IUnitOfWork unitOfWork,
    Mediator.IMediator mediator)
    : ICommandHandler<UpdateRepositoryCleanupOptionsCommand, RepositoryCleanupOptions>
{
    public async ValueTask<Result<RepositoryCleanupOptions>> Handle(UpdateRepositoryCleanupOptionsCommand request, CancellationToken ct)
    {
        await repository.UpsertAsync(RepositoryCleanupOptions.SectionName, JsonSerializer.Serialize(request.Options), ct);
        unitOfWork.OnAfterSave(() => store.Invalidate(RepositoryCleanupOptions.SectionName));

        await mediator.Publish(new ConfigurationUpdatedNotification(), ct);

        return Result<RepositoryCleanupOptions>.Success(request.Options);
    }
}