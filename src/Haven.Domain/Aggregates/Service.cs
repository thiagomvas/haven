using System.Text.Json;
using Haven.Domain.Aggregates;
using Haven.Domain.Events;
using Haven.Domain.Exceptions;
using Haven.Domain.ValueObjects;

namespace Haven.Domain.Entities;

public sealed class Service : AggregateRoot, ISoftDeletable
{
    public Guid EnvironmentId { get; set; }
    public Environment? Environment { get; set; }
    public string Name { get; set; } = default!;
    public ServiceType Type { get; set; }
    public ExposureMode ExposureMode { get; set; }
    public ServiceStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? SourceConfigJson { get; set; }
    public ServiceSourceConfig? SourceConfig
    {
        get => SourceConfigJson is null ? null : JsonSerializer.Deserialize<ServiceSourceConfig>(SourceConfigJson);
        set => SourceConfigJson = value is null ? null : JsonSerializer.Serialize(value);
    }

    public IReadOnlyList<ServiceNetwork> ServiceNetworks => _serviceNetworks.AsReadOnly();
    private List<ServiceNetwork> _serviceNetworks = [];

    private static readonly HashSet<string> ReservedNames =
        new(StringComparer.OrdinalIgnoreCase) { "haven", "dns", "localhost", "host", "internal" };

    public static Service Create(Guid environmentId, string name, ServiceType type, ExposureMode exposureMode, ServiceSourceConfig? sourceConfig = null)
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
            Type = type,
            ExposureMode = exposureMode,
            SourceConfigJson = Serialize(sourceConfig),
            Status = ServiceStatus.Stopped,
            CreatedAt = now,
            UpdatedAt = now
        };
        
        service.Raise(new ServiceCreatedEvent(service.Id, service.Name));
        return service;
    }

    public bool Update(Optional<string> name, Optional<ServiceType> type, Optional<ExposureMode> exposureMode, Optional<ServiceSourceConfig?> sourceConfig = default)
    {
        bool hasChanges = false;

        if (name.HasValue && name.Value != Name)
        {
            _ = HavenServiceName.From(name.Value);

            if (ReservedNames.Contains(name.Value))
                throw new ValidationException($"'{name.Value}' is a reserved service name and cannot be used.");

            Name = name.Value;
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
            Raise(new ServiceUpdatedEvent(Id, Name));
        }

        return hasChanges;
    }

    public void MarkDeployed()
    {
        Status = ServiceStatus.Running;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkStopped()
    {
        if (Status == ServiceStatus.Stopped)
            throw new ValidationException($"Service '{Name}' is already stopped.");

        Status = ServiceStatus.Stopped;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsDegraded()
    {
        Status = ServiceStatus.Degraded;
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
    
    public static Service Reconstitute(
        Guid id,
        Guid environmentId,
        string name,
        ServiceType type,
        ExposureMode exposureMode,
        ServiceStatus status,
        DateTime createdAt,
        DateTime updatedAt,
        ServiceSourceConfig? sourceConfig = null,
        Environment? environment = null,
        IEnumerable<ServiceNetwork>? serviceNetworks = null)
    {
        return new Service
        {
            Id = id,
            EnvironmentId = environmentId,
            Environment = environment,
            Name = name,
            Type = type,
            ExposureMode = exposureMode,
            Status = status,
            SourceConfigJson = Serialize(sourceConfig),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            _serviceNetworks = serviceNetworks?.ToList() ?? []
        };
    }

    private static string? Serialize(ServiceSourceConfig? config) =>
        config is null ? null : JsonSerializer.Serialize(config);

    public void Delete()
    {
        Raise(new ServiceDeletedEvent(Id, Name));
    }
}
