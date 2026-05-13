using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.FeatureFlags.Commands.DeleteFeatureFlagCommand;

public class DeleteFeatureFlagCommand : ICommand
{
    public Guid FlagId { get; set; }
}
