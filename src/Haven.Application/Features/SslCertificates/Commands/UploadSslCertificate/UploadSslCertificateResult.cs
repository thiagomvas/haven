namespace Haven.Application.Features.SslCertificates.Commands.UploadSslCertificate;

public sealed class UploadSslCertificateResult
{
    public Guid CertificateId { get; set; }
    public DateTimeOffset NotAfter { get; set; }

    /// <summary>Non-blocking warning if the uploaded certificate is already expired.</summary>
    public List<string> Warnings { get; set; } = [];
}