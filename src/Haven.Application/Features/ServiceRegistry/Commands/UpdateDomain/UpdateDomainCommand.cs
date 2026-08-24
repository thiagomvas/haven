using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Features.ServiceRegistry.Commands.UpdateDomain;

/// <summary>Exactly one of <see cref="ServiceId"/>/<see cref="SidecarId"/> must be provided.</summary>
[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class UpdateDomainCommand : ICommand
{
    public Optional<Guid> ServiceId { get; set; }
    public Optional<Guid> SidecarId { get; set; }
    public Guid DomainId { get; set; }
    public string? Hostname { get; set; }
    public int? ContainerPort { get; set; }
    public TlsMode? TlsMode { get; set; }
}