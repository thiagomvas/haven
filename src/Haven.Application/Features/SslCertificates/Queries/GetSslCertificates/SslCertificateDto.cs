namespace Haven.Application.Features.SslCertificates.Queries.GetSslCertificates;

public sealed class SslCertificateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SubjectCommonName { get; set; }
    public DateTimeOffset NotBefore { get; set; }
    public DateTimeOffset NotAfter { get; set; }
    public bool IsExpired { get; set; }
    public int AttachedDomainCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}