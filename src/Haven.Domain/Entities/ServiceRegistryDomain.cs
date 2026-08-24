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

    public static ServiceRegistryDomain Create(Guid serviceRegistryEntryId, string hostname, int containerPort, TlsMode tlsMode = TlsMode.None)
    {
        hostname = Normalize(hostname);
        Validate(hostname, containerPort);

        var now = DateTime.UtcNow;
        return new ServiceRegistryDomain
        {
            ServiceRegistryEntryId = serviceRegistryEntryId,
            Hostname = hostname,
            ContainerPort = containerPort,
            TlsMode = tlsMode,
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
        DateTime updatedAt)
    {
        var domain = new ServiceRegistryDomain
        {
            ServiceRegistryEntryId = serviceRegistryEntryId,
            Hostname = hostname,
            ContainerPort = containerPort,
            TlsMode = tlsMode,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
        domain.Id = id;
        return domain;
    }

    /// <summary>
    /// Applies a partial update, re-validating and re-normalizing the resulting hostname.
    /// </summary>
    internal void Apply(Optional<string> hostname, Optional<int> containerPort, Optional<TlsMode> tlsMode = default)
    {
        var newHostname = hostname.HasValue ? Normalize(hostname.Value) : Hostname;
        var newContainerPort = containerPort.HasValue ? containerPort.Value : ContainerPort;

        Validate(newHostname, newContainerPort);

        Hostname = newHostname;
        ContainerPort = newContainerPort;
        if (tlsMode.HasValue)
            TlsMode = tlsMode.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string Normalize(string hostname) => hostname?.Trim().ToLowerInvariant() ?? string.Empty;

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