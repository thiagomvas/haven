using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.ServiceRegistry.Commands.DetachDomainCertificate;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class DetachDomainCertificateCommand : ICommand
{
    /// <summary>
    /// Owner-agnostic - domain ids are globally unique, so this works for service- and
    /// sidecar-owned domains alike.
    /// </summary>
    public Guid DomainId { get; set; }
}