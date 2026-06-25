using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Commands.ApplyManifestForService;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class ApplyManifestForServiceCommand : ICommand
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public Guid ServiceId { get; set; }
    public string ManifestYaml { get; set; } = string.Empty;
}