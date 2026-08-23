using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Commands.CloneService;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class CloneServiceCommand : ICommand<Guid>, IMutatesManifestState
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public Guid ServiceId { get; set; }
    public string NewName { get; set; } = string.Empty;
    public string? NewAlias { get; set; }
    public Guid? TargetProjectId { get; set; }
    public Guid? TargetEnvironmentId { get; set; }
}