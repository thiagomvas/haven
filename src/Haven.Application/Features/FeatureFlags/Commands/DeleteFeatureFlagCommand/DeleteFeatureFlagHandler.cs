using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

namespace Haven.Application.Features.FeatureFlags.Commands.DeleteFeatureFlagCommand;

public class DeleteFeatureFlagHandler(
    IFeatureFlagRepository featureFlagRepository,
    IServiceRepository serviceRepository)
    : ICommandHandler<DeleteFeatureFlagCommand>
{
    public async ValueTask<Result> Handle(DeleteFeatureFlagCommand command, CancellationToken cancellationToken)
    {
        var flag = await featureFlagRepository.GetByIdAsync(command.FlagId, cancellationToken);
        if (flag is null)
            return Error.NotFoundFor(nameof(FeatureFlag), command.FlagId);

        var service = await serviceRepository.GetByIdAsync(flag.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), flag.ServiceId);

        service.RemoveFeatureFlag(flag);
        await featureFlagRepository.RemoveAsync(flag, cancellationToken);
        return Result.Success();
    }
}