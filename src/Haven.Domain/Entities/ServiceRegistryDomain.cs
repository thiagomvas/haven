using System.Text.Json.Serialization;

using Haven.Domain.Aggregates;
using Haven.Domain.Exceptions;

namespace Haven.Domain.Entities;

public sealed class ServiceRegistryDomain : Entity
{
    public Guid ServiceRegistryEntryId { get; set; }
    public string Hostname { get; set; } = default!;
    public int ContainerPort { get; set; }
    public bool EnableTls { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [JsonIgnore] public ServiceRegistryEntry? ServiceRegistryEntry { get; set; }

    private ServiceRegistryDomain() { }

    public static ServiceRegistryDomain Create(Guid serviceRegistryEntryId, string hostname, int containerPort, bool enableTls = false)
    {
        hostname = Normalize(hostname);
        Validate(hostname, containerPort);

        var now = DateTime.UtcNow;
        return new ServiceRegistryDomain
        {
            ServiceRegistryEntryId = serviceRegistryEntryId,
            Hostname = hostname,
            ContainerPort = containerPort,
            EnableTls = enableTls,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static ServiceRegistryDomain Reconstitute(
        Guid id,
        Guid serviceRegistryEntryId,
        string hostname,
        int containerPort,
        bool enableTls,
        DateTime createdAt,
        DateTime updatedAt)
    {
        var domain = new ServiceRegistryDomain
        {
            ServiceRegistryEntryId = serviceRegistryEntryId,
            Hostname = hostname,
            ContainerPort = containerPort,
            EnableTls = enableTls,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
        domain.Id = id;
        return domain;
    }

    /// <summary>
    /// Applies a partial update, re-validating and re-normalizing the resulting hostname.
    /// </summary>
    internal void Apply(Optional<string> hostname, Optional<int> containerPort, Optional<bool> enableTls = default)
    {
        var newHostname = hostname.HasValue ? Normalize(hostname.Value) : Hostname;
        var newContainerPort = containerPort.HasValue ? containerPort.Value : ContainerPort;

        Validate(newHostname, newContainerPort);

        Hostname = newHostname;
        ContainerPort = newContainerPort;
        if (enableTls.HasValue)
            EnableTls = enableTls.Value;
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