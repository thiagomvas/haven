using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.CustomActions.Commands.UpdateCustomAction;

[RequirePermission(Permissions.ProjectManagement.ManageConfig)]
public sealed class UpdateCustomActionCommand : ICommand, IMutatesManifestState
{
    public Guid ServiceId { get; set; }
    public Guid ActionId { get; set; }
    public string? ActionName { get; set; }
    public string? Alias { get; set; }
    public string? ActionDescription { get; set; }
    public string? Icon { get; set; }
    public ActionConfig? Config { get; set; }
    public string[]? RequiredPermissions { get; set; }
    public ActionRisk? Risk { get; set; }
    public TimeSpan? Timeout { get; set; }
    public CustomActionInput[]? Inputs { get; set; }
}
