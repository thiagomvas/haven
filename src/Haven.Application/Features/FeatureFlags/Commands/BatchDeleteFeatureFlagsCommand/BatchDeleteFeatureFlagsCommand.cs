using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.FeatureFlags.Commands.BatchDeleteFeatureFlagsCommand;

[RequirePermission(Permissions.FeatureFlags.Delete)]
public class BatchDeleteFeatureFlagsCommand : ICommand
{
    public IReadOnlyList<Guid> FlagIds { get; set; } = [];
}
