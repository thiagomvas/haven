using Haven.Domain.Enums;

namespace Haven.Application.Features.ServiceRegistry.Queries.GetServiceRegistryEntries;

public sealed class ServiceRegistryDomainDto
{
    public Guid Id { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public int ContainerPort { get; set; }
    public TlsMode TlsMode { get; set; }
    public bool HasCertificate { get; set; }
    public Guid? CertificateId { get; set; }
    public string? CertificateName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}