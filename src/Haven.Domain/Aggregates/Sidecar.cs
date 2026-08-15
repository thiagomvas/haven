using System.Text.Json;

using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.Events;
using Haven.Domain.ValueObjects;

namespace Haven.Domain.Aggregates;

/// <summary>
/// A Haven-managed container that extends the platform itself,
/// as opposed to a <see cref="Service"/> which belongs to a user's Project/Environment. Sidecars are
/// opt-in, admin-only, and always exist independently of any Project or Environment.
/// </summary>
public sealed class Sidecar : AggregateRoot, IDeployableContainer
{
    public string Name { get; set; } = default!;
    public string? Alias { get; set; }
    public SidecarKind Kind { get; set; }
    public ServiceStatus Status { get; set; }
    public ServiceHealth Health { get; set; }

    /// <summary>
    /// Whether an admin has opted into this sidecar. Sidecars are opt-out by default: they are
    /// registered but never deployed until explicitly enabled.
    /// </summary>
    public bool Enabled { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastDeployedAt { get; set; }

    public string? SourceConfigJson { get; set; }
    public ServiceSourceConfig? SourceConfig
    {
        get => SourceConfigJson is null ? null : JsonSerializer.Deserialize<ServiceSourceConfig>(SourceConfigJson);
        set => SourceConfigJson = value is null ? null : JsonSerializer.Serialize(value);
    }

    public IReadOnlyList<SidecarNetwork> SidecarNetworks => _sidecarNetworks.AsReadOnly();
    private List<SidecarNetwork> _sidecarNetworks = [];

    private Sidecar() { }

    public static Sidecar Create(string name, SidecarKind kind, string? alias = null, ServiceSourceConfig? sourceConfig = null)
    {
        HavenServiceName.EnsureValidAndNotReserved(name);

        var now = DateTime.UtcNow;
        var sidecar = new Sidecar
        {
            Id = Guid.NewGuid(),
            Name = name,
            Alias = alias,
            Kind = kind,
            SourceConfigJson = Serialize(sourceConfig),
            Status = ServiceStatus.Stopped,
            Health = ServiceHealth.Unknown,
            Enabled = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        sidecar.Raise(new SidecarCreatedEvent(sidecar.Id, sidecar.Name));
        return sidecar;
    }

    public static Sidecar Reconstitute(
        Guid id,
        string name,
        string? alias,
        SidecarKind kind,
        ServiceStatus status,
        ServiceHealth health,
        bool enabled,
        DateTime createdAt,
        DateTime updatedAt,
        DateTime? lastDeployedAt = null,
        ServiceSourceConfig? sourceConfig = null,
        IEnumerable<SidecarNetwork>? sidecarNetworks = null)
    {
        return new Sidecar
        {
            Id = id,
            Name = name,
            Alias = alias,
            Kind = kind,
            Status = status,
            Health = health,
            Enabled = enabled,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            LastDeployedAt = lastDeployedAt,
            SourceConfigJson = Serialize(sourceConfig),
            _sidecarNetworks = sidecarNetworks?.ToList() ?? []
        };
    }

    public bool Update(Optional<string> name, Optional<string?> alias = default, Optional<ServiceSourceConfig?> sourceConfig = default)
    {
        var oldName = Name;
        bool hasChanges = false;

        if (name.HasValue && name.Value != Name)
        {
            HavenServiceName.EnsureValidAndNotReserved(name.Value);
            Name = name.Value;
            hasChanges = true;
        }

        if (alias.HasValue && alias.Value != Alias)
        {
            Alias = alias.Value;
            hasChanges = true;
        }

        if (sourceConfig.HasValue)
        {
            SourceConfigJson = Serialize(sourceConfig.Value);
            hasChanges = true;
        }

        if (hasChanges)
        {
            UpdatedAt = DateTime.UtcNow;
            Raise(new SidecarUpdatedEvent(Id, oldName, Name));
        }
        return hasChanges;
    }

    public void Enable()
    {
        if (Enabled) return;
        Enabled = true;
        UpdatedAt = DateTime.UtcNow;
        Raise(new SidecarEnabledEvent(Id, Name));
    }

    public void Disable()
    {
        if (!Enabled) return;
        Enabled = false;
        Status = ServiceStatus.Stopped;
        UpdatedAt = DateTime.UtcNow;
        Raise(new SidecarDisabledEvent(Id, Name));
    }

    public void MarkDeploymentPending()
    {
        Status = ServiceStatus.DeploymentPending;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkDeploying()
    {
        Status = ServiceStatus.Deploying;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkDeployed()
    {
        if (Status == ServiceStatus.Running) return;
        Status = ServiceStatus.Running;
        var now = DateTime.UtcNow;
        UpdatedAt = now;
        LastDeployedAt = now;
    }

    public void MarkStopped()
    {
        if (Status == ServiceStatus.Stopped) return;
        Status = ServiceStatus.Stopped;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordHealth(ServiceHealth health)
    {
        Health = health;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AttachToNetwork(Guid networkId)
    {
        if (_sidecarNetworks.Any(sn => sn.NetworkId == networkId))
            return;

        _sidecarNetworks.Add(SidecarNetwork.Create(Id, networkId));
        UpdatedAt = DateTime.UtcNow;
        Raise(new SidecarAttachedEvent(Id, Name, networkId));
    }

    public void DetachFromNetwork(Guid networkId)
    {
        var connection = _sidecarNetworks.FirstOrDefault(sn => sn.NetworkId == networkId);
        if (connection is null)
            return;

        _sidecarNetworks.Remove(connection);
        UpdatedAt = DateTime.UtcNow;
        Raise(new SidecarDetachedEvent(Id, Name, networkId));
    }

    public void Delete()
    {
        Raise(new SidecarDeletedEvent(Id, Name));
    }

    private static string? Serialize(ServiceSourceConfig? config) =>
        config is null ? null : JsonSerializer.Serialize(config);
}
