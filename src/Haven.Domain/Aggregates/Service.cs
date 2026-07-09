using System.Text.Json;

using Haven.Domain.Aggregates;
using Haven.Domain.Events;
using Haven.Domain.Exceptions;
using Haven.Domain.ValueObjects;

namespace Haven.Domain.Entities;

public sealed class Service : AggregateRoot
{
    public Guid EnvironmentId { get; set; }
    public Environment? Environment { get; set; }
    public string Name { get; set; } = default!;
    public string? Alias { get; set; }
    public ServiceType Type { get; set; }
    public ExposureMode ExposureMode { get; set; }
    public ServiceStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastDeployedAt { get; set; }
    public string Token { get; set; } = default!;
    public string? SourceConfigJson { get; set; }
    public Guid? GitCredentialId { get; set; } = null;
    public ServiceSourceConfig? SourceConfig
    {
        get => SourceConfigJson is null ? null : JsonSerializer.Deserialize<ServiceSourceConfig>(SourceConfigJson);
        set => SourceConfigJson = value is null ? null : JsonSerializer.Serialize(value);
    }

    public IReadOnlyList<ServiceNetwork> ServiceNetworks => _serviceNetworks.AsReadOnly();
    private List<ServiceNetwork> _serviceNetworks = [];

    public ICollection<ServiceVolume> Volumes { get; set; } = [];

    public ICollection<Deployment> Deployments { get; set; } = [];
    public ICollection<FeatureFlag> FeatureFlags { get; set; } = [];
    public GitCredentials? GitCredentials { get; set; } = null;

    private static readonly HashSet<string> ReservedNames =
        new(StringComparer.OrdinalIgnoreCase) { "haven", "dns", "localhost", "host", "internal" };

    public static Service Create(Guid environmentId, string name, ServiceType type, ExposureMode exposureMode, string? alias = null, ServiceSourceConfig? sourceConfig = null)
    {
        _ = HavenServiceName.From(name);

        if (ReservedNames.Contains(name))
            throw new ValidationException($"'{name}' is a reserved service name and cannot be used.");

        var now = DateTime.UtcNow;
        var service = new Service
        {
            Id = Guid.NewGuid(),
            EnvironmentId = environmentId,
            Name = name,
            Alias = alias,
            Type = type,
            ExposureMode = exposureMode,
            Token = GenerateToken(),
            SourceConfigJson = Serialize(sourceConfig),
            Status = ServiceStatus.Stopped,
            CreatedAt = now,
            UpdatedAt = now
        };

        service.Raise(new ServiceCreatedEvent(service.Id, service.Name));
        return service;
    }

    public bool Update(Optional<string> name, Optional<ServiceType> type, Optional<ExposureMode> exposureMode, Optional<string> alias = default, Optional<ServiceSourceConfig?> sourceConfig = default)
    {
        var oldName = Name;
        bool hasChanges = false;

        if (name.HasValue && name.Value != Name)
        {
            _ = HavenServiceName.From(name.Value);

            if (ReservedNames.Contains(name.Value))
                throw new ValidationException($"'{name.Value}' is a reserved service name and cannot be used.");

            Name = name.Value;
            hasChanges = true;
        }

        if (alias.HasValue && alias.Value != Alias)
        {
            Alias = alias.Value;
            hasChanges = true;
        }

        if (type.HasValue && type.Value != Type)
        {
            Type = type.Value;
            hasChanges = true;
        }

        if (exposureMode.HasValue && exposureMode.Value != ExposureMode)
        {
            ExposureMode = exposureMode.Value;
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
            Raise(new ServiceUpdatedEvent(Id, oldName, Name));
        }
        return hasChanges;
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
        Raise(new ServiceDeployingEvent(Id, Name));
    }

    public void MarkDeployed()
    {
        if (Status == ServiceStatus.Running) return;
        Status = ServiceStatus.Running;
        var now = DateTime.UtcNow;
        UpdatedAt = now;
        LastDeployedAt = now;
        Raise(new ServiceDeployedEvent(Id, Name));
    }

    public void MarkStopped()
    {
        if (Status == ServiceStatus.Stopped) return;
        Status = ServiceStatus.Stopped;
        UpdatedAt = DateTime.UtcNow;
        Raise(new ServiceStoppedEvent(Id, Name));
    }

    public void MarkAsDegraded()
    {
        Status = ServiceStatus.Degraded;
        UpdatedAt = DateTime.UtcNow;
        Raise(new ServiceDegradedEvent(Id, Name));
    }

    public void RegenerateToken()
    {
        Token = GenerateToken();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restart()
    {
        Status = ServiceStatus.Running;
        UpdatedAt = DateTime.UtcNow;
        Raise(new ServiceRestartedEvent(Id, Name));
    }

    public void ConnectToNetwork(Guid networkId)
    {
        if (!_serviceNetworks.Any(sn => sn.NetworkId == networkId))
            _serviceNetworks.Add(ServiceNetwork.Create(Id, networkId));
    }

    public void DisconnectFromNetwork(Guid networkId)
    {
        var connection = _serviceNetworks.FirstOrDefault(sn => sn.NetworkId == networkId);
        if (connection is not null)
            _serviceNetworks.Remove(connection);
    }


    public void UpdateEnvironmentVariables()
    {
        Raise(new EnvironmentVariablesUpdatedEvent(Id, EnvironmentVariableParentType.Environment));
    }

    public static Service Reconstitute(
        Guid id,
        Guid environmentId,
        string name,
        string? alias,
        ServiceType type,
        ExposureMode exposureMode,
        ServiceStatus status,
        DateTime createdAt,
        DateTime updatedAt,
        ServiceSourceConfig? sourceConfig = null,
        Environment? environment = null,
        IEnumerable<ServiceNetwork>? serviceNetworks = null,
        IEnumerable<ServiceVolume>? volumes = null)
    {
        var service = new Service
        {
            Id = id,
            EnvironmentId = environmentId,
            Environment = environment,
            Name = name,
            Alias = alias,
            Type = type,
            ExposureMode = exposureMode,
            Status = status,
            SourceConfigJson = Serialize(sourceConfig),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            _serviceNetworks = serviceNetworks?.ToList() ?? [],
            Volumes = volumes?.ToList() ?? []
        };

        if (string.IsNullOrEmpty(service.Token))
            service.Token = GenerateNewToken();

        return service;
    }

    private static string? Serialize(ServiceSourceConfig? config) =>
        config is null ? null : JsonSerializer.Serialize(config);

    public static string GenerateNewToken() =>
        Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string GenerateToken() =>
        GenerateNewToken();

    public void Delete()
    {
        Raise(new ServiceDeletedEvent(Id, Name));
    }

    public FeatureFlag AddFeatureFlag(string name, FeatureFlagType type, string? key, string? description, string value, FeatureFlagValueType valueType)
    {
        var flag = FeatureFlag.Create(Id, name, type, key, description, value, valueType);
        FeatureFlags.Add(flag);
        return flag;
    }

    public void UpdateFeatureFlag(FeatureFlag flag, Optional<string> name, Optional<FeatureFlagType> type, Optional<string?> key, Optional<string?> description, Optional<string> value, Optional<FeatureFlagValueType> valueType)
    {
        if (name.HasValue && name.Value != flag.Name)
            flag.Name = name.Value;

        if (type.HasValue && type.Value != flag.Type)
            flag.Type = type.Value;

        if (key.HasValue && key.Value != flag.Key)
            flag.Key = key.Value;

        if (description.HasValue && description.Value != flag.Description)
            flag.Description = description.Value;

        if (value.HasValue && value.Value != flag.Value)
            flag.Value = value.Value;

        if (valueType.HasValue && valueType.Value != flag.ValueType)
            flag.ValueType = valueType.Value;
    }

    public void RemoveFeatureFlag(FeatureFlag flag)
    {
        FeatureFlags.Remove(flag);
    }

    public ServiceVolume AddVolume(VolumeType type, string name, string target, string? source = null, bool readOnly = false, bool backupEnabled = false)
    {
        EnsureTargetNotTaken(target, excludingVolumeId: null);

        var volume = ServiceVolume.Create(Id, type, name, target, source, readOnly, backupEnabled);
        Volumes.Add(volume);
        UpdatedAt = DateTime.UtcNow;
        Raise(new ServiceUpdatedEvent(Id, Name, Name));
        return volume;
    }

    public void UpdateVolume(ServiceVolume volume, Optional<string> name, Optional<string> source, Optional<string> target, Optional<bool> readOnly, Optional<bool> backupEnabled)
    {
        if (!Volumes.Contains(volume))
            throw new ValidationException("The volume does not belong to this service.");

        if (target.HasValue)
            EnsureTargetNotTaken(target.Value, excludingVolumeId: volume.Id);

        volume.Apply(name, source, target, readOnly, backupEnabled);
        UpdatedAt = DateTime.UtcNow;
        Raise(new ServiceUpdatedEvent(Id, Name, Name));
    }

    /// <summary>
    /// Ensures no other volume on this service already mounts to <paramref name="target"/>,
    /// since Docker rejects two mounts at the same container path.
    /// </summary>
    private void EnsureTargetNotTaken(string target, Guid? excludingVolumeId)
    {
        var trimmedTarget = target?.Trim() ?? string.Empty;
        if (Volumes.Any(v => v.Id != excludingVolumeId && v.Target == trimmedTarget))
            throw new ValidationException($"A volume with target '{trimmedTarget}' already exists on this service.");
    }

    public void RemoveVolume(ServiceVolume volume)
    {
        if (Volumes.Remove(volume))
        {
            UpdatedAt = DateTime.UtcNow;
            Raise(new ServiceUpdatedEvent(Id, Name, Name));
        }
    }
}