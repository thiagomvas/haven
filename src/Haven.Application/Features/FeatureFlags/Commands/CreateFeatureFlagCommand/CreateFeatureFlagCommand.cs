using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.FeatureFlags.Commands.CreateFeatureFlagCommand;

public class CreateFeatureFlagCommand : ICommand<Guid>
{
    public Guid ServiceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public FeatureFlagType Type { get; set; }
    public string? Description { get; set; }
    public string Value { get; set; } = string.Empty;
    public FeatureFlagValueType ValueType { get; set; }
}
