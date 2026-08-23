using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Environments.Commands.CreateEnvironment;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class CreateEnvironmentCommand : ICommand<Guid>, IMutatesManifestState
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public string? Description { get; set; }
}