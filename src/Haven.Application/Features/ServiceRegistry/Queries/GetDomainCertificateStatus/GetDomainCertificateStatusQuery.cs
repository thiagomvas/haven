using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.ServiceRegistry.Queries.GetDomainCertificateStatus;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetDomainCertificateStatusQuery : IQuery<DomainCertificateStatusDto>
{
    public Guid ServiceId { get; set; }
    public Guid DomainId { get; set; }
}
