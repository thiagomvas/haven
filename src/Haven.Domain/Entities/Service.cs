using Haven.Domain.Exceptions;
using Haven.Domain.ValueObjects;

namespace Haven.Domain.Entities;

public sealed class Service : Entity
{
    public Guid EnvironmentId { get; private set; }
    public string Name { get; private set; } = default!;
    public ServiceType Type { get; private set; }
    public ExposureMode ExposureMode { get; private set; }
    public ServiceStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private static readonly HashSet<string> ReservedNames =
        new(StringComparer.OrdinalIgnoreCase) { "haven", "dns", "localhost", "host", "internal" };

    internal static Service Create(Guid environmentId, string name, ServiceType type, ExposureMode exposureMode)
    {
        _ = HavenServiceName.From(name);

        if (ReservedNames.Contains(name))
            throw new ValidationException($"'{name}' is a reserved service name and cannot be used.");

        var now = DateTime.UtcNow;
        return new Service
        {
            Id = Guid.NewGuid(),
            EnvironmentId = environmentId,
            Name = name,
            Type = type,
            ExposureMode = exposureMode,
            Status = ServiceStatus.Stopped,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    internal bool Update(Optional<string> name, Optional<ServiceType> type, Optional<ExposureMode> exposureMode)
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

        if (hasChanges)
            UpdatedAt = DateTime.UtcNow;

        return hasChanges;
    }

    internal void MarkDeployed()
    {
        Status = ServiceStatus.Running;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void MarkStopped()
    {
        if (Status == ServiceStatus.Stopped)
            throw new ValidationException($"Service '{Name}' is already stopped.");

        Status = ServiceStatus.Stopped;
        UpdatedAt = DateTime.UtcNow;
    }

    internal static Service Reconstitute(
        Guid id,
        Guid environmentId,
        string name,
        ServiceType type,
        ExposureMode exposureMode,
        ServiceStatus status,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new Service
        {
            Id = id,
            EnvironmentId = environmentId,
            Name = name,
            Type = type,
            ExposureMode = exposureMode,
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }
}
