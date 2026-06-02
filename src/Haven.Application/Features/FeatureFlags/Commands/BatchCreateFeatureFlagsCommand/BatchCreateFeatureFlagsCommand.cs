using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.FeatureFlags.Commands.CreateFeatureFlagCommand;

namespace Haven.Application.Features.FeatureFlags.Commands.BatchCreateFeatureFlagsCommand;

[RequirePermission(Permissions.FeatureFlags.Create)]
public class BatchCreateFeatureFlagsCommand : ICommand<IReadOnlyList<Guid>>
{
    public IReadOnlyList<CreateFeatureFlagCommand.CreateFeatureFlagCommand> Creates { get; set; } = [];
}
