using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.CustomActions.Commands.ExecuteCustomAction;

[RequirePermission(Permissions.ProjectManagement.ManageDeploys)]
public sealed class ExecuteCustomActionCommand : ICommand
{
    public Guid ServiceId { get; set; }
    public Guid ActionId { get; set; }
    public Dictionary<string, string>? Inputs { get; set; }
    public string Token { get; set; } = string.Empty;
}
