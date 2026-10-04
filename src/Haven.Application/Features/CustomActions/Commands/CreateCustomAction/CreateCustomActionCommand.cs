using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.CustomActions.Commands.CreateCustomAction;

[RequirePermission(Permissions.ProjectManagement.ManageConfig)]
public sealed class CreateCustomActionCommand : ICommand<Guid>, IMutatesManifestState
{
    public Guid ServiceId { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string ActionDescription { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public ActionConfig Config { get; set; } = default!;
    public string[] RequiredPermissions { get; set; } = [];
    public ActionRisk Risk { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public CustomActionInput[] Inputs { get; set; } = [];
}
