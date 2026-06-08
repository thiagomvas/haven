using System.Text.Json;
using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

namespace Haven.Application.Features.Setup.Commands.ConfigureInstanceCommand;

public class ConfigureInstanceHandler(
    IHavenService havenService,
    IHavenSettingRepository repository,
    IHavenConfigurationStore store)
    : ICommandHandler<ConfigureInstanceCommand>
{
    public async ValueTask<Result> Handle(ConfigureInstanceCommand command, CancellationToken cancellationToken)
    {
        var stage = await havenService.GetSetupStageAsync(cancellationToken);
        if (stage != SetupStage.NotStarted)
            return Error.Failure("Setup.InstanceAlreadyConfigured", "Instance has already been configured.");

        var options = new InstanceOptions { InstanceName = command.InstanceName, Timezone = command.Timezone, TimeFormat = command.TimeFormat };
        await repository.UpsertAsync(InstanceOptions.SectionName, JsonSerializer.Serialize(options), cancellationToken);
        store.Invalidate(InstanceOptions.SectionName);

        await havenService.AdvanceSetupStageAsync(SetupStage.InstanceConfigured, cancellationToken);
        return Result.Success();
    }
}
