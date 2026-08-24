using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.ServiceRegistry.Commands.UploadDomainCertificate;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class UploadDomainCertificateCommand : ICommand<UploadDomainCertificateResult>
{
    public Guid ServiceId { get; set; }
    public Guid DomainId { get; set; }

    /// <summary>
    /// The certificate (and any intermediate chain) PEM, either pasted directly or read client-side
    /// from an uploaded file via <c>FileReader</c> - both paths submit the same text field.
    /// </summary>
    public string CertificatePem { get; set; } = string.Empty;

    /// <summary>
    /// The private key PEM, pasted or uploaded the same way as <see cref="CertificatePem"/>.
    /// </summary>
    public string PrivateKeyPem { get; set; } = string.Empty;
}
