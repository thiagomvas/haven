using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.ServiceRegistry.Commands.DeleteDomain;

/// <summary>Exactly one of <see cref="ServiceId"/>/<see cref="SidecarId"/> must be provided.</summary>
[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class DeleteDomainCommand : ICommand
{
    public Optional<Guid> ServiceId { get; set; }
    public Optional<Guid> SidecarId { get; set; }
    public Guid DomainId { get; set; }
}