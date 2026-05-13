using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.FeatureFlags.Commands.BatchDeleteFeatureFlagsCommand;

public class BatchDeleteFeatureFlagsCommand : ICommand
{
    public IReadOnlyList<Guid> FlagIds { get; set; } = [];
}
