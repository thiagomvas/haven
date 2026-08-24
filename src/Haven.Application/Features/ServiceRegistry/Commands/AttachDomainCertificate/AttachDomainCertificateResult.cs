namespace Haven.Application.Features.ServiceRegistry.Commands.AttachDomainCertificate;

public sealed class AttachDomainCertificateResult
{
    public Guid CertificateId { get; set; }
    public DateTimeOffset NotAfter { get; set; }

    /// <summary>
    /// Non-blocking warnings about the attached certificate - e.g. it's already expired, or its
    /// CN/SANs don't include the domain's hostname. The attach still succeeds; these are surfaced
    /// so the UI can flag them without preventing the save.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}
