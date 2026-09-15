using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Commands.ExportServiceToDockerCompose;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class ExportServiceToDockerComposeCommand : ICommand<string>
{
    public Guid ServiceId { get; set; }
}
