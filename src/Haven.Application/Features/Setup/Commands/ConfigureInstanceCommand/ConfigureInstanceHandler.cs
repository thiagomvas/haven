using System.Text.Json;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.Configuration.Events;

namespace Haven.Application.Features.Setup.Commands.ConfigureInstanceCommand;

public class ConfigureInstanceHandler(
    IHavenService havenService,
    IHavenSettingRepository repository,
    IHavenConfigurationStore store,
    IUnitOfWork unitOfWork,
    Mediator.IMediator mediator)
    : ICommandHandler<ConfigureInstanceCommand>
{
    public async ValueTask<Result> Handle(ConfigureInstanceCommand command, CancellationToken cancellationToken)
    {
        var stage = await havenService.GetSetupStageAsync(cancellationToken);
        if (stage != SetupStage.NotStarted)
            return Error.OperationAlreadyDone;

        var options = new InstanceOptions { InstanceName = command.InstanceName, Timezone = command.Timezone, TimeFormat = command.TimeFormat };
        await repository.UpsertAsync(InstanceOptions.SectionName, JsonSerializer.Serialize(options), cancellationToken);
        unitOfWork.OnAfterSave(() => store.Invalidate(InstanceOptions.SectionName));

        await mediator.Publish(new ConfigurationUpdatedNotification(), cancellationToken);

        await havenService.AdvanceSetupStageAsync(SetupStage.InstanceConfigured, cancellationToken);
        return Result.Success();
    }
}