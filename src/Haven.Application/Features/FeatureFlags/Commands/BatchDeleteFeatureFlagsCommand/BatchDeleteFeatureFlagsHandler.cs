using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

namespace Haven.Application.Features.FeatureFlags.Commands.BatchDeleteFeatureFlagsCommand;

public class BatchDeleteFeatureFlagsHandler(
    IFeatureFlagRepository featureFlagRepository,
    IServiceRepository serviceRepository)
    : ICommandHandler<BatchDeleteFeatureFlagsCommand>
{
    public async ValueTask<Result> Handle(BatchDeleteFeatureFlagsCommand command, CancellationToken cancellationToken)
    {
        foreach (var flagId in command.FlagIds)
        {
            var flag = await featureFlagRepository.GetByIdAsync(flagId, cancellationToken);
            if (flag is null)
                return Error.NotFoundFor(nameof(FeatureFlag), flagId);

            var service = await serviceRepository.GetByIdAsync(flag.ServiceId, cancellationToken);
            if (service is null)
                return Error.NotFoundFor(nameof(Service), flag.ServiceId);

            service.RemoveFeatureFlag(flag);
            await featureFlagRepository.RemoveAsync(flag, cancellationToken);
        }

        return Result.Success();
    }
}