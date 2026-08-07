using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

namespace Haven.Application.Features.FeatureFlags.Commands.UpdateFeatureFlagCommand;

public class UpdateFeatureFlagHandler(
    IFeatureFlagRepository featureFlagRepository,
    IServiceRepository serviceRepository)
    : ICommandHandler<UpdateFeatureFlagCommand>
{
    public async ValueTask<Result> Handle(UpdateFeatureFlagCommand command,
        CancellationToken cancellationToken)
    {
        var flag = await featureFlagRepository.GetByIdAsync(command.FlagId, cancellationToken);
        if (flag is null)
            return Error.NotFoundFor(nameof(FeatureFlag), command.FlagId);

        var service = await serviceRepository.GetByIdAsync(flag.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), flag.ServiceId);

        service.UpdateFeatureFlag(flag, command.Name, command.Type.ToOptional(), command.Key, command.Description, command.Value, command.ValueType.ToOptional());

        return Result.Success();
    }
}