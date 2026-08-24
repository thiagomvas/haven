using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.ServiceRegistry.Commands.AttachDomainCertificate;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class AttachDomainCertificateCommand : ICommand<AttachDomainCertificateResult>
{
    public Guid ServiceId { get; set; }
    public Guid DomainId { get; set; }

    /// <summary>The library <see cref="Haven.Domain.Entities.SslCertificate"/> to attach.</summary>
    public Guid CertificateId { get; set; }
}
