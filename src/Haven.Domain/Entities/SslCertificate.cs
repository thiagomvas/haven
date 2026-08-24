using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

using Haven.Domain.Exceptions;
using Haven.Domain.ValueObjects;

namespace Haven.Domain.Entities;

/// <summary>
/// A user-supplied ("bring your own") TLS certificate/key pair, stored once and attachable to any
/// number of <see cref="ServiceRegistryDomain"/>s whose <c>TlsMode</c> is <c>Custom</c> - e.g. a
/// wildcard certificate reused across several subdomains instead of being re-uploaded per domain.
/// The private key is encrypted at rest; the certificate (and any chain) is public material and
/// stored in plaintext.
/// </summary>
public sealed class SslCertificate : Entity
{
    public string Name { get; private set; } = default!;
    public string CertificatePem { get; private set; } = default!;
    public EncryptedValue PrivateKeyPem { get; private set; } = default!;
    public DateTimeOffset NotBefore { get; private set; }
    public DateTimeOffset NotAfter { get; private set; }
    public string? SubjectCommonName { get; private set; }
    public string Fingerprint { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    [JsonIgnore] public ICollection<ServiceRegistryDomain> Domains { get; set; } = [];

    private SslCertificate() { }

    public static SslCertificate Create(string name, string certificatePem, string privateKeyPem)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Certificate name is required.");

        var (notBefore, notAfter, subjectCommonName, fingerprint) = ParsePem(certificatePem, privateKeyPem);

        var now = DateTimeOffset.UtcNow;
        return new SslCertificate
        {
            Name = name.Trim(),
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
    /// re-deriving the parsed metadata. Rotating a shared library certificate updates it for every
    /// domain it's attached to.
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

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Certificate name is required.");

        Name = name.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsExpired => NotAfter < DateTimeOffset.UtcNow;

    /// <summary>
    /// Whether the given hostname matches this certificate's CN or any SAN entry, including a
    /// single-level RFC 6125 wildcard (e.g. <c>*.example.com</c> matches <c>app.example.com</c> but
    /// not <c>example.com</c> or <c>a.b.example.com</c>). Computed on demand rather than stored, so
    /// it can never go stale relative to a domain's hostname changes.
    /// </summary>
    public bool MatchesHostname(string hostname)
    {
        using var cert = X509Certificate2.CreateFromPem(CertificatePem);

        if (MatchesName(cert.GetNameInfo(X509NameType.DnsName, false), hostname))
            return true;

        foreach (var extension in cert.Extensions)
        {
            if (extension is not X509SubjectAlternativeNameExtension sanExtension)
                continue;

            foreach (var dnsName in sanExtension.EnumerateDnsNames())
            {
                if (MatchesName(dnsName, hostname))
                    return true;
            }
        }

        return false;
    }

    private static bool MatchesName(string? certName, string hostname)
    {
        if (string.IsNullOrEmpty(certName))
            return false;

        if (string.Equals(certName, hostname, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!certName.StartsWith("*.", StringComparison.Ordinal))
            return false;

        // Single-level wildcard only: the wildcard covers exactly one leftmost label of the
        // hostname and never matches the bare parent domain or a deeper nested subdomain.
        var suffix = certName[1..]; // ".example.com"
        var hostnameLabelEnd = hostname.IndexOf('.');
        if (hostnameLabelEnd <= 0)
            return false;

        var hostnameSuffix = hostname[hostnameLabelEnd..];
        return string.Equals(hostnameSuffix, suffix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parses and validates a certificate/key pair, throwing <see cref="ValidationException"/> for
    /// structural problems (unparseable PEM, mismatched cert/key). Does NOT reject an already-expired
    /// certificate - expiry is surfaced to the caller as data (<see cref="IsExpired"/>) rather than
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
