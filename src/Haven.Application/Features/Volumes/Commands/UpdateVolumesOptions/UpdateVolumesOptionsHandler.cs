using System.Text.Json;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.Configuration.Events;

namespace Haven.Application.Features.Volumes.Commands.UpdateVolumesOptions;

public sealed class UpdateVolumesOptionsHandler(
    IHavenSettingRepository repository,
    IHavenConfigurationStore store,
    IUnitOfWork unitOfWork,
    Mediator.IMediator mediator)
    : ICommandHandler<UpdateVolumesOptionsCommand, VolumesOptions>
{
    public async ValueTask<Result<VolumesOptions>> Handle(UpdateVolumesOptionsCommand request, CancellationToken ct)
    {
        await repository.UpsertAsync(VolumesOptions.SectionName, JsonSerializer.Serialize(request.Options), ct);
        unitOfWork.OnAfterSave(() => store.Invalidate(VolumesOptions.SectionName));

        await mediator.Publish(new ConfigurationUpdatedNotification(), ct);

        return Result<VolumesOptions>.Success(request.Options);
    }
}