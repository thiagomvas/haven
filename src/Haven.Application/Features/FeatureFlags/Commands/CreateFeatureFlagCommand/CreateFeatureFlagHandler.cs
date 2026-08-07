using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

namespace Haven.Application.Features.FeatureFlags.Commands.CreateFeatureFlagCommand;

public class CreateFeatureFlagHandler(
    IServiceRepository serviceRepository,
    IFeatureFlagRepository featureFlagRepository)
    : ICommandHandler<CreateFeatureFlagCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreateFeatureFlagCommand command, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(command.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), command.ServiceId);

        var flag = service.AddFeatureFlag(
            command.Name,
            command.Type,
            command.Key,
            command.Description,
            command.Value,
            command.ValueType);

        await featureFlagRepository.AddAsync(flag, cancellationToken);
        return Result<Guid>.CreatedFor(flag.Id);
    }
}