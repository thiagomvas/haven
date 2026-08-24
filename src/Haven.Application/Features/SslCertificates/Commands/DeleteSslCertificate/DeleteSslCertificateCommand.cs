using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.SslCertificates.Commands.DeleteSslCertificate;

[RequirePermission(Permissions.ProjectManagement.Delete)]
public sealed class DeleteSslCertificateCommand : ICommand
{
    public Guid CertificateId { get; set; }
}
