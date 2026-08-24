using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Features.ServiceRegistry.Commands.AddDomain;

/// <summary>
/// Exactly one of <see cref="ServiceId"/>/<see cref="SidecarId"/> must be provided - see
/// <see cref="AddDomainValidator"/>. The sidecar path is currently restricted to
/// <see cref="Haven.Domain.Enums.SidecarKind.Traefik"/> (its dashboard) by <c>AddDomainHandler</c>.
/// </summary>
[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class AddDomainCommand : ICommand<Guid>
{
    public Optional<Guid> ServiceId { get; set; }
    public Optional<Guid> SidecarId { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public int ContainerPort { get; set; }
    public TlsMode TlsMode { get; set; } = TlsMode.None;
}