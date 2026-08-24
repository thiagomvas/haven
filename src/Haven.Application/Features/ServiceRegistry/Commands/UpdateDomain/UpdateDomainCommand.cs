using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain.Enums;

namespace Haven.Application.Features.ServiceRegistry.Commands.UpdateDomain;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class UpdateDomainCommand : ICommand
{
    public Guid ServiceId { get; set; }
    public Guid DomainId { get; set; }
    public string? Hostname { get; set; }
    public int? ContainerPort { get; set; }
    public TlsMode? TlsMode { get; set; }
}