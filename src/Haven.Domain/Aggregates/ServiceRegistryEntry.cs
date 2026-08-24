using System.Text.Json.Serialization;

using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.Exceptions;
using Haven.Domain.ValueObjects;

namespace Haven.Domain.Aggregates;

public class ServiceRegistryEntry : AggregateRoot
{
    public Guid ServiceId { get; set; }
    public string? ContainerName { get; set; }
    public string? IpAddress { get; set; }
    public List<PortMapping> Ports { get; set; } = [];
    public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;
    public DateTime RegisteredAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? StartedAt { get; set; }

    public ICollection<ServiceRegistryDomain> Domains { get; set; } = [];

    [JsonIgnore] public Service? Service { get; set; }

    public static ServiceRegistryEntry Create(Guid serviceId)
    {
        var now = DateTime.UtcNow;
        return new ServiceRegistryEntry
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            RegisteredAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateFromService(Service service)
    {
        Status = service.Status;
        if (service.Status == ServiceStatus.Running)
            StartedAt ??= DateTime.UtcNow;
        else
            StartedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRuntime(string ip, List<PortMapping> ports, ServiceStatus status)
    {
        IpAddress = ip;
        Ports = ports;
        Status = status;
        if (status == ServiceStatus.Running)
            StartedAt ??= DateTime.UtcNow;
        else
            StartedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkStopped()
    {
        Status = ServiceStatus.Stopped;
        StartedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public ServiceRegistryDomain AddDomain(string hostname, int containerPort, TlsMode tlsMode = TlsMode.None)
    {
        EnsureHostnameNotTakenLocally(hostname, excludingDomainId: null);

        var domain = ServiceRegistryDomain.Create(Id, hostname, containerPort, tlsMode);
        Domains.Add(domain);
        UpdatedAt = DateTime.UtcNow;
        return domain;
    }

    public void UpdateDomain(ServiceRegistryDomain domain, Optional<string> hostname, Optional<int> containerPort, Optional<TlsMode> tlsMode = default)
    {
        if (!Domains.Contains(domain))
            throw new ValidationException("The domain does not belong to this service registry entry.");

        if (hostname.HasValue)
            EnsureHostnameNotTakenLocally(hostname.Value, excludingDomainId: domain.Id);

        domain.Apply(hostname, containerPort, tlsMode);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveDomain(ServiceRegistryDomain domain)
    {
        if (Domains.Remove(domain))
            UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Guards only against duplicate hostnames within this entry's own domains. The authoritative,
    /// instance-wide uniqueness check happens in the application layer (AddDomainHandler/UpdateDomainHandler)
    /// via a repository lookup across all registry entries, plus a DB-level unique index as the final guarantee.
    /// </summary>
    private void EnsureHostnameNotTakenLocally(string hostname, Guid? excludingDomainId)
    {
        var normalized = hostname?.Trim().ToLowerInvariant() ?? string.Empty;
        if (Domains.Any(d => d.Id != excludingDomainId && d.Hostname == normalized))
            throw new ValidationException($"A domain with hostname '{normalized}' already exists on this service.");
    }
}