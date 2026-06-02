using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.FeatureFlags.Commands.BatchUpdateFeatureFlagsCommand;

[RequirePermission(Permissions.FeatureFlags.Update)]
public class BatchUpdateFeatureFlagsCommand : ICommand<IReadOnlyList<Guid>>
{
    public IReadOnlyList<UpdateFeatureFlagCommand.UpdateFeatureFlagCommand> Updates { get; set; } = [];
}
