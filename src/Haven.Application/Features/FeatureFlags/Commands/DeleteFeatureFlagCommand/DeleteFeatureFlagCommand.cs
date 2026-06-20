using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.FeatureFlags.Commands.DeleteFeatureFlagCommand;

[RequirePermission(Permissions.ProjectManagement.ManageConfig)]
public class DeleteFeatureFlagCommand : ICommand
{
    public Guid FlagId { get; set; }
}