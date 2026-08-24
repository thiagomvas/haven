using System.Text.Json.Serialization;

using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Domain.Exceptions;

namespace Haven.Domain.Entities;

public sealed class ServiceRegistryDomain : Entity
{
    public Guid ServiceRegistryEntryId { get; set; }
    public string Hostname { get; set; } = default!;
    public int ContainerPort { get; set; }
    public TlsMode TlsMode { get; set; }

    /// <summary>
    /// Optional forwarding rewrite: when set, this path is prepended to every request's path
    /// before it reaches the container (Traefik's <c>AddPrefix</c> middleware), letting a domain
    /// transparently address a sub-path on the container without the client ever typing it (e.g.
    /// <c>api.example.com/users</c> forwarded as <c>/api/v1/users</c>). Always starts with
    /// <c>/</c> and never ends with one; <see langword="null"/> means no rewrite.
    /// </summary>
    public string? InternalBasePath { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [JsonIgnore] public ServiceRegistryEntry? ServiceRegistryEntry { get; set; }
    public Guid? SslCertificateId { get; set; }
    public SslCertificate? Certificate { get; set; }

    /// <summary>
    /// The Traefik router name Docker labels are built under for this domain (see
    /// <c>DockerUtils.BuildTraefikLabels</c>) - derived from <see cref="Entity.Id"/> rather than
    /// <see cref="Hostname"/>, since hostnames aren't safe as Traefik resource identifiers and can
    /// change via <c>UpdateDomain</c>.
    /// </summary>
    [JsonIgnore] public string RouterName => $"haven-{Id:N}";

    /// <summary>The HTTPS-entrypoint router name used when <see cref="TlsMode"/> is not <c>None</c>.</summary>
    [JsonIgnore] public string SecureRouterName => $"{RouterName}-secure";

    private ServiceRegistryDomain() { }

    public static ServiceRegistryDomain Create(Guid serviceRegistryEntryId, string hostname, int containerPort, TlsMode tlsMode = TlsMode.None, string? internalBasePath = null)
    {
        hostname = Normalize(hostname);
        internalBasePath = NormalizeBasePath(internalBasePath);
        Validate(hostname, containerPort);

        var now = DateTime.UtcNow;
        return new ServiceRegistryDomain
        {
            ServiceRegistryEntryId = serviceRegistryEntryId,
            Hostname = hostname,
            ContainerPort = containerPort,
            TlsMode = tlsMode,
            InternalBasePath = internalBasePath,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static ServiceRegistryDomain Reconstitute(
        Guid id,
        Guid serviceRegistryEntryId,
        string hostname,
        int containerPort,
        TlsMode tlsMode,
        DateTime createdAt,
        DateTime updatedAt,
        string? internalBasePath = null)
    {
        var domain = new ServiceRegistryDomain
        {
            ServiceRegistryEntryId = serviceRegistryEntryId,
            Hostname = hostname,
            ContainerPort = containerPort,
            TlsMode = tlsMode,
            InternalBasePath = internalBasePath,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
        domain.Id = id;
        return domain;
    }

    /// <summary>
    /// Applies a partial update, re-validating and re-normalizing the resulting hostname.
    /// </summary>
    internal void Apply(Optional<string> hostname, Optional<int> containerPort, Optional<TlsMode> tlsMode = default, Optional<string> internalBasePath = default)
    {
        var newHostname = hostname.HasValue ? Normalize(hostname.Value) : Hostname;
        var newContainerPort = containerPort.HasValue ? containerPort.Value : ContainerPort;
        var newInternalBasePath = internalBasePath.HasValue ? NormalizeBasePath(internalBasePath.Value) : InternalBasePath;

        Validate(newHostname, newContainerPort);

        Hostname = newHostname;
        ContainerPort = newContainerPort;
        InternalBasePath = newInternalBasePath;
        if (tlsMode.HasValue)
            TlsMode = tlsMode.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string Normalize(string hostname) => hostname?.Trim().ToLowerInvariant() ?? string.Empty;

    /// <summary>
    /// Normalizes an internal base path: blank or a bare "/" means "no rewrite" and collapses to
    /// <see langword="null"/>, otherwise the path must start with "/" and any trailing "/" is stripped.
    /// </summary>
    private static string? NormalizeBasePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        path = path.Trim();
        if (path == "/")
            return null;

        if (!path.StartsWith('/'))
            throw new ValidationException($"Internal base path '{path}' must start with '/'.");

        return path.TrimEnd('/');
    }

    private static void Validate(string hostname, int containerPort)
    {
        if (string.IsNullOrWhiteSpace(hostname))
            throw new ValidationException("Domain hostname is required.");

        if (Uri.CheckHostName(hostname) == UriHostNameType.Unknown)
            throw new ValidationException($"'{hostname}' is not a valid hostname.");

        if (containerPort is < 1 or > 65535)
            throw new ValidationException("Container port must be between 1 and 65535.");
    }
}