using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

using Haven.Domain.Exceptions;
using Haven.Domain.ValueObjects;

namespace Haven.Domain.Entities;

/// <summary>
/// A user-supplied ("bring your own") TLS certificate/key pair attached to a single
/// <see cref="ServiceRegistryDomain"/>. Only relevant when that domain's <c>TlsMode</c> is
/// <c>Custom</c>. The private key is encrypted at rest; the certificate (and any chain) is public
/// material and stored in plaintext.
/// </summary>
public sealed class DomainCertificate : Entity
{
    public Guid ServiceRegistryDomainId { get; private set; }
    public string CertificatePem { get; private set; } = default!;
    public EncryptedValue PrivateKeyPem { get; private set; } = default!;
    public DateTimeOffset NotBefore { get; private set; }
    public DateTimeOffset NotAfter { get; private set; }
    public string? SubjectCommonName { get; private set; }
    public string Fingerprint { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    [JsonIgnore] public ServiceRegistryDomain? ServiceRegistryDomain { get; set; }

    private DomainCertificate() { }

    public static DomainCertificate Create(Guid serviceRegistryDomainId, string certificatePem, string privateKeyPem)
    {
        var (notBefore, notAfter, subjectCommonName, fingerprint) = ParsePem(certificatePem, privateKeyPem);

        var now = DateTimeOffset.UtcNow;
        return new DomainCertificate
        {
            ServiceRegistryDomainId = serviceRegistryDomainId,
            CertificatePem = certificatePem,
            PrivateKeyPem = EncryptedValue.From(privateKeyPem),
            NotBefore = notBefore,
            NotAfter = notAfter,
            SubjectCommonName = subjectCommonName,
            Fingerprint = fingerprint,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Replaces the certificate/key pair in place (re-upload/rotation), re-validating and
    /// re-deriving the parsed metadata.
    /// </summary>
    public void Rotate(string certificatePem, string privateKeyPem)
    {
        var (notBefore, notAfter, subjectCommonName, fingerprint) = ParsePem(certificatePem, privateKeyPem);

        CertificatePem = certificatePem;
        PrivateKeyPem = EncryptedValue.From(privateKeyPem);
        NotBefore = notBefore;
        NotAfter = notAfter;
        SubjectCommonName = subjectCommonName;
        Fingerprint = fingerprint;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsExpired => NotAfter < DateTimeOffset.UtcNow;

    /// <summary>
    /// Whether the given hostname matches this certificate's CN or any SAN entry. Computed on
    /// demand rather than stored, so it can never go stale relative to a domain's hostname changes.
    /// </summary>
    public bool MatchesHostname(string hostname)
    {
        using var cert = X509Certificate2.CreateFromPem(CertificatePem);
        if (string.Equals(cert.GetNameInfo(X509NameType.DnsName, false), hostname, StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (var extension in cert.Extensions)
        {
            if (extension is not X509SubjectAlternativeNameExtension sanExtension)
                continue;

            foreach (var dnsName in sanExtension.EnumerateDnsNames())
            {
                if (string.Equals(dnsName, hostname, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Parses and validates a certificate/key pair, throwing <see cref="ValidationException"/> for
    /// structural problems (unparseable PEM, mismatched cert/key). Does NOT reject an already-expired
    /// certificate — expiry is surfaced to the caller as data (<see cref="IsExpired"/>) rather than
    /// blocked, matching the guard-rail's warn-only stance.
    /// </summary>
    private static (DateTimeOffset notBefore, DateTimeOffset notAfter, string? subjectCommonName, string fingerprint) ParsePem(
        string certificatePem, string privateKeyPem)
    {
        if (string.IsNullOrWhiteSpace(certificatePem))
            throw new ValidationException("Certificate PEM is required.");

        if (string.IsNullOrWhiteSpace(privateKeyPem))
            throw new ValidationException("Private key PEM is required.");

        X509Certificate2 cert;
        try
        {
            cert = X509Certificate2.CreateFromPem(certificatePem, privateKeyPem);
        }
        catch (Exception ex)
        {
            throw new ValidationException("The certificate and private key could not be parsed, or do not match.", ex);
        }

        using (cert)
        {
            return (
                new DateTimeOffset(cert.NotBefore.ToUniversalTime()),
                new DateTimeOffset(cert.NotAfter.ToUniversalTime()),
                cert.GetNameInfo(X509NameType.SimpleName, false),
                cert.Thumbprint);
        }
    }
}
