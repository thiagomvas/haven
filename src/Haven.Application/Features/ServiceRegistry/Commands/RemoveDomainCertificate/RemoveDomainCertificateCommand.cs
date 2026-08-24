using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.ServiceRegistry.Commands.RemoveDomainCertificate;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class RemoveDomainCertificateCommand : ICommand
{
    public Guid ServiceId { get; set; }
    public Guid DomainId { get; set; }
}
