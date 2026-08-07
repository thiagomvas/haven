using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

namespace Haven.Application.Features.FeatureFlags.Commands.BatchUpdateFeatureFlagsCommand;

public class BatchUpdateFeatureFlagsHandler(
    IFeatureFlagRepository featureFlagRepository,
    IServiceRepository serviceRepository)
    : ICommandHandler<BatchUpdateFeatureFlagsCommand, IReadOnlyList<Guid>>
{
    public async ValueTask<Result<IReadOnlyList<Guid>>> Handle(
        BatchUpdateFeatureFlagsCommand command,
        CancellationToken cancellationToken)
    {
        var updatedIds = new List<Guid>(command.Updates.Count);

        foreach (var update in command.Updates)
        {
            var flag = await featureFlagRepository.GetByIdAsync(update.FlagId, cancellationToken);
            if (flag is null)
                return Error.NotFoundFor(nameof(FeatureFlag), update.FlagId);

            var service = await serviceRepository.GetByIdAsync(flag.ServiceId, cancellationToken);
            if (service is null)
                return Error.NotFoundFor(nameof(Service), flag.ServiceId);

            service.UpdateFeatureFlag(flag, update.Name, update.Type.ToOptional(), update.Key, update.Description, update.Value, update.ValueType.ToOptional());

            updatedIds.Add(flag.Id);
        }

        return Result<IReadOnlyList<Guid>>.Success(updatedIds.AsReadOnly());
    }
}