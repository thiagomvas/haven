using System.Text.Json;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.Configuration.Dtos;

namespace Haven.Application.Features.Configuration.Commands.UpdateConfiguration;

public sealed class UpdateConfigurationHandler(
    IHavenSettingRepository repository,
    IHavenConfigurationStore store,
    IHavenConfigurationSerializer serializer)
    : ICommandHandler<UpdateConfigurationCommand, HavenConfigurationDto>
{
    public async ValueTask<Result<HavenConfigurationDto>> Handle(
        UpdateConfigurationCommand request,
        CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(request.Manifests);
        await repository.UpsertAsync(ManifestsOptions.SectionName, json, ct);

        var config = new HavenConfiguration { Manifests = request.Manifests };
        await serializer.WriteAsync(config, ct);

        store.Invalidate(ManifestsOptions.SectionName);

        var dto = new HavenConfigurationDto(request.Manifests);
        return Result<HavenConfigurationDto>.Success(dto);
    }
}