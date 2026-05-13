using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;

namespace Haven.Application.Features.FeatureFlags.Commands.BatchCreateFeatureFlagsCommand;

public class BatchCreateFeatureFlagsHandler(
    IServiceRepository serviceRepository,
    IFeatureFlagRepository featureFlagRepository)
    : ICommandHandler<BatchCreateFeatureFlagsCommand, IReadOnlyList<Guid>>
{
    public async ValueTask<Result<IReadOnlyList<Guid>>> Handle(
        BatchCreateFeatureFlagsCommand command,
        CancellationToken cancellationToken)
    {
        var createdIds = new List<Guid>(command.Creates.Count);

        foreach (var create in command.Creates)
        {
            var service = await serviceRepository.GetByIdAsync(create.ServiceId, cancellationToken);
            if (service is null)
                return Error.NotFoundFor(nameof(Service), create.ServiceId);

            var flag = service.AddFeatureFlag(
                create.Name,
                create.Type,
                create.Key,
                create.Description,
                create.Value,
                create.ValueType);

            await featureFlagRepository.AddAsync(flag, cancellationToken);
            createdIds.Add(flag.Id);
        }

        return Result<IReadOnlyList<Guid>>.CreatedFor(createdIds.AsReadOnly());
    }
}
