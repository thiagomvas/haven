using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.FeatureFlags.Commands.UpdateFeatureFlagCommand;

[RequirePermission(Permissions.ProjectManagement.ManageConfig)]
public class UpdateFeatureFlagCommand : ICommand
{
    public Guid FlagId { get; set; }
    public string? Name { get; set; }
    public FeatureFlagType? Type { get; set; }
    public string? Key { get; set; }
    public string? Description { get; set; }
    public string? Value { get; set; }
    public FeatureFlagValueType? ValueType { get; set; }
}