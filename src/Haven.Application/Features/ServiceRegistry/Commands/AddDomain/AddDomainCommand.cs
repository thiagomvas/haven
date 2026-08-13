using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.ServiceRegistry.Commands.AddDomain;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class AddDomainCommand : ICommand<Guid>
{
    public Guid ServiceId { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public int ContainerPort { get; set; }
}