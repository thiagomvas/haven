using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;
using Haven.Domain.Entities;

namespace Haven.Application.Features.FeatureFlags.Commands.UpdateFeatureFlagCommand;

public class UpdateFeatureFlagHandler(IFeatureFlagRepository repository)
    : ICommandHandler<UpdateFeatureFlagCommand>
{
    public async ValueTask<Result> Handle(UpdateFeatureFlagCommand command,
        CancellationToken cancellationToken)
    {
        var flag = await repository.GetByIdAsync(command.FlagId, cancellationToken);
        if (flag is null) return Error.NotFoundFor(nameof(FeatureFlag), command.FlagId);

        flag.Ingest(command);

        return Result.Success();
    }
}