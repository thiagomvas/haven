using System.Text.Json;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.Configuration.Events;

namespace Haven.Application.Features.Setup.Commands.ConfigureNetworkCommand;

public class ConfigureNetworkHandler(
    IHavenService havenService,
    IHavenSettingRepository repository,
    IHavenConfigurationStore store,
    IUnitOfWork unitOfWork,
    Mediator.IMediator mediator)
    : ICommandHandler<ConfigureNetworkCommand>
{
    public async ValueTask<Result> Handle(ConfigureNetworkCommand command, CancellationToken cancellationToken)
    {
        var stage = await havenService.GetSetupStageAsync(cancellationToken);
        if (stage != SetupStage.SuperUserCreated)
            return Error.InvalidOperation("Super user must be created before configuring network access.");

        var options = new NetworkOptions
        {
            Domains = command.Domain is { Length: > 0 } d ? [d] : [],
            Port = command.Port ?? 8080,
            EnableTls = command.EnableTls,
        };
        await repository.UpsertAsync(NetworkOptions.SectionName, JsonSerializer.Serialize(options), cancellationToken);
        unitOfWork.OnAfterSave(() => store.Invalidate(NetworkOptions.SectionName));

        await mediator.Publish(new ConfigurationUpdatedNotification(), cancellationToken);

        await havenService.AdvanceSetupStageAsync(SetupStage.Completed, cancellationToken);
        return Result.Success();
    }
}