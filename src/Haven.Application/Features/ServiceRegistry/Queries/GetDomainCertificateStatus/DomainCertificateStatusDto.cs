using Haven.Domain.Enums;

namespace Haven.Application.Features.ServiceRegistry.Queries.GetDomainCertificateStatus;

public sealed class DomainCertificateStatusDto
{
    public TlsMode TlsMode { get; set; }

    /// <summary>"Database" for Custom-mode certs (Haven's own source of truth), "TraefikApi" for Acme-mode.</summary>
    public string SourceOfTruth { get; set; } = string.Empty;

    public DateTimeOffset? NotBefore { get; set; }
    public DateTimeOffset? NotAfter { get; set; }
    public string? SubjectCommonName { get; set; }
    public bool IsExpired { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public bool HostnameMismatch { get; set; }

    /// <summary>Only meaningful for Acme mode - whether Haven could reach Traefik's API at all.</summary>
    public bool TraefikReachable { get; set; } = true;

    public string? RouterStatus { get; set; }
    public List<string> Errors { get; set; } = [];
}
