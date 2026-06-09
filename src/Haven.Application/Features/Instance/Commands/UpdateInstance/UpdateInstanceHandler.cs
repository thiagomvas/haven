using System.Text.Json;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.Instance.Dtos;

namespace Haven.Application.Features.Instance.Commands.UpdateInstance;

public sealed class UpdateInstanceHandler(
    IHavenSettingRepository repository,
    IHavenConfigurationStore store)
    : ICommandHandler<UpdateInstanceCommand, InstanceDto>
{
    public async ValueTask<Result<InstanceDto>> Handle(UpdateInstanceCommand command, CancellationToken ct)
    {
        var options = new InstanceOptions
        {
            InstanceName = command.InstanceName,
            Timezone = command.Timezone,
            TimeFormat = command.TimeFormat,
        };

        await repository.UpsertAsync(InstanceOptions.SectionName, JsonSerializer.Serialize(options), ct);
        store.Invalidate(InstanceOptions.SectionName);

        var dto = new InstanceDto(options.InstanceName, options.Timezone, options.TimeFormat);
        return Result<InstanceDto>.Success(dto);
    }
}