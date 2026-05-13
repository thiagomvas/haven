using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;
using Haven.Domain.Entities;

namespace Haven.Application.Features.FeatureFlags.Commands.BatchUpdateFeatureFlagsCommand;

public class BatchUpdateFeatureFlagsHandler(IFeatureFlagRepository repository)
    : ICommandHandler<BatchUpdateFeatureFlagsCommand, IReadOnlyList<Guid>>
{
    public async ValueTask<Result<IReadOnlyList<Guid>>> Handle(
        BatchUpdateFeatureFlagsCommand command,
        CancellationToken cancellationToken)
    {
        var updatedIds = new List<Guid>(command.Updates.Count);

        foreach (var update in command.Updates)
        {
            var flag = await repository.GetByIdAsync(update.FlagId, cancellationToken);
            if (flag is null)
                return Error.NotFoundFor(nameof(FeatureFlag), update.FlagId);

            flag.Ingest(update);
            updatedIds.Add(flag.Id);
        }

        return Result<IReadOnlyList<Guid>>.Success(updatedIds.AsReadOnly());
    }
}
