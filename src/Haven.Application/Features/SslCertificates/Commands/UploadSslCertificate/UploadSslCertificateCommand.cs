using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.SslCertificates.Commands.UploadSslCertificate;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class UploadSslCertificateCommand : ICommand<UploadSslCertificateResult>
{
    /// <summary>A human-friendly label for the dropdown, e.g. "Wildcard *.example.com".</summary>
    public string Name { get; set; } = string.Empty;

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