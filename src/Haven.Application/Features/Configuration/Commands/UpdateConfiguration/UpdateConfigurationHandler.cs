using System.Text.Json;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.Configuration.Dtos;
using Haven.Application.Features.Configuration.Events;

namespace Haven.Application.Features.Configuration.Commands.UpdateConfiguration;

public sealed class UpdateConfigurationHandler(
    IHavenSettingRepository repository,
    IHavenConfigurationStore store,
    IUnitOfWork unitOfWork,
    Mediator.IMediator mediator)
    : ICommandHandler<UpdateConfigurationCommand, HavenConfigurationDto>
{
    public async ValueTask<Result<HavenConfigurationDto>> Handle(UpdateConfigurationCommand request, CancellationToken ct)
    {
        await repository.UpsertAsync(ManifestsOptions.SectionName, JsonSerializer.Serialize(request.Manifests), ct);
        unitOfWork.OnAfterSave(() => store.Invalidate(ManifestsOptions.SectionName));

        await mediator.Publish(new ConfigurationUpdatedNotification(), ct);

        return Result<HavenConfigurationDto>.Success(new HavenConfigurationDto(request.Manifests));
    }
}
