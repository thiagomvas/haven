using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.ServiceRegistry.Commands.AttachDomainCertificate;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class AttachDomainCertificateCommand : ICommand<AttachDomainCertificateResult>
{
    /// <summary>
    /// Owner-agnostic - domain ids are globally unique, so this works for service- and
    /// sidecar-owned domains alike (see <see cref="Haven.Application.Common.Interfaces.Repositories.IServiceRegistryEntryRepository.GetByDomainIdAsync"/>).
    /// </summary>
    public Guid DomainId { get; set; }

    /// <summary>The library <see cref="Haven.Domain.Entities.SslCertificate"/> to attach.</summary>
    public Guid CertificateId { get; set; }
}
