using Haven.Domain.Enums;
using Haven.Domain.Events;
using Haven.Domain.Exceptions;
using Haven.Domain.ValueObjects;

namespace Haven.Domain.Aggregates;

/// <summary>
/// Represents a deployment context within a Project, e.g. dev, staging, production.
/// Owns a set of Services and a dedicated Docker network that isolates them
/// from services in other environments by default.
/// </summary>
public sealed class Environment : AggregateRoot
{
    /// <summary>
    /// Foreign key to the owning Project.
    /// Required by EF Core for the relationship, and used by event handlers
    /// that need to identify the parent project without loading the full aggregate.
    /// </summary>
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    /// <summary>
    /// The deployment context label, e.g. "dev", "staging", "prod".
    /// Unique within a project but not globally, so two projects can both have a "staging" environment.
    /// Haven enforces no naming convention, teams choose their own labels.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Short alias used in Docker resource names. 2–8 lowercase alphanumeric or hyphen characters.
    /// Unique within the owning project.
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// Optional free-text description of the environment.
    /// No functional role, purely informational for the dashboard.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The actual Docker network name provisioned when this environment was created.
    /// Never user-supplied and derived deterministically from the project ID and environment name,
    /// e.g. "haven_a1b2c3d4_staging". Stored so Haven can reconnect containers to the correct
    /// network without reconstructing the name from parts at runtime.
    /// </summary>
    public string NetworkName { get; set; } = default!;

    public IReadOnlyList<Service> Services => _services.AsReadOnly();
    private List<Service> _services = [];

    public const int MaxNameLength = 64;
    public const int MaxDescriptionLength = 250;

    private static readonly HashSet<string> ReservedNames =
        new(StringComparer.OrdinalIgnoreCase) { "haven", "shared", "internal", "host" };

    public static Environment Create(Guid projectId, string name, string? alias = null, string? projectAlias = null, string? description = null)
    {
        var id = Guid.NewGuid();
        var networkName = (alias != null && projectAlias != null)
            ? BuildNetworkName(projectAlias, alias)
            : $"{DomainConstants.NetworkBaseName}-{projectId.ToString("N")[..8]}-{DomainConstants.Slugify(name)}";

        var result = new Environment()
        {
            Id = id,
            ProjectId = projectId,
            Name = name,
            Alias = alias,
            Description = description,
            NetworkName = networkName
        };

        result.Raise(new EnvironmentCreatedEvent(result.Id, result.Name));
        return result;
    }

    public (bool HasChanges, string OldName) Update(Optional<string> name, Optional<string> alias, Optional<string?> description)
    {
        var oldName = Name;
        bool hasChanges = false;

        if (name.HasValue && name.Value != Name)
        {
            Name = name.Value;
            hasChanges = true;
        }

        if (alias.HasValue && alias.Value != Alias)
        {
            Alias = alias.Value;
            hasChanges = true;
        }

        if (description.HasValue && description.Value != Description)
        {
            Description = description.Value;
            hasChanges = true;
        }

        Raise(new EnvironmentUpdatedEvent(Id, oldName, Name));
        return (hasChanges, oldName);
    }

    public static string BuildNetworkName(string projectAlias, string envAlias)
    {
        return $"{DomainConstants.NetworkBaseName}-{projectAlias}-{envAlias}";
    }

    public Service AddService(string name, ServiceType type, ExposureMode exposureMode, string? alias = null, ServiceSourceConfig? sourceConfig = null)
    {
        if (_services.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new ValidationException($"A service named '{name}' already exists in environment '{Name}'.");

        var service = Service.Create(Id, name, type, exposureMode, alias, sourceConfig);
        _services.Add(service);
        service.Environment = this;
        return service;
    }

    public bool UpdateService(Guid serviceId, Optional<string> name, Optional<ServiceType> type, Optional<ExposureMode> exposureMode, Optional<string> alias = default, Optional<ServiceSourceConfig?> sourceConfig = default)
    {
        var service = GetService(serviceId);
        return service.Update(name, type, exposureMode, alias, sourceConfig);
    }

    public Service RemoveService(Guid serviceId)
    {
        var service = GetService(serviceId);

        if (service.Status == ServiceStatus.Running)
            throw new ValidationException($"Service '{service.Name}' is currently running and cannot be removed.");

        _services.Remove(service);
        return service;
    }

    public void MarkServiceDeploymentPending(Guid serviceId) => GetService(serviceId).MarkDeploymentPending();

    public void DeployService(Guid serviceId) => GetService(serviceId).MarkDeployed();

    public void StopService(Guid serviceId) => GetService(serviceId).MarkStopped();

    public void RestartService(Guid serviceId) => GetService(serviceId).Restart();

    private Service GetService(Guid serviceId) =>
        _services.Find(s => s.Id == serviceId)
            ?? throw new NotFoundException($"Service '{serviceId}' not found in environment '{Name}'.");

    public void UpdateEnvironmentVariables()
    {
        Raise(new EnvironmentVariablesUpdatedEvent(Id, EnvironmentVariableParentType.Environment));
    }

    public static Environment Reconstitute(Guid id, Guid projectId, string name, string? alias, string? description, string networkName, IEnumerable<Service>? services = null, Project? project = null)
    {
        return new Environment
        {
            Id = id,
            ProjectId = projectId,
            Project = project,
            Name = name,
            Alias = alias,
            Description = description,
            NetworkName = networkName,
            _services = services?.ToList() ?? []
        };
    }

    public void Delete()
    {
        foreach (var service in Services)
        {
            service.Delete();
        }

        Raise(new EnvironmentDeletedEvent(Id, Name));
    }

    public int GetRunningServicesCount() => Services.Count(s => s.Status is ServiceStatus.Running);
    public HealthStatus GetStatus()
    {
        if (Services.Count == 0)
            return HealthStatus.Unknown;
        var runningCount = GetRunningServicesCount();
        var total = Services.Count;

        if (runningCount == total)
            return HealthStatus.Healthy;
        if (runningCount > 0)
            return HealthStatus.Degraded;
        if (Services.Any(s => s.Status is ServiceStatus.Deploying))
            return HealthStatus.Deploying;
        if (Services.Any(s => s.Status is ServiceStatus.DeploymentPending))
            return HealthStatus.DeploymentPending;

        return HealthStatus.Stopped;
    }

    public (int Total, int Running, int Stopped, int Degraded, int DeploymentPending, int Deploying, int Unknown) GetServiceStatistics()
    {
        var total = Services.Count;
        var running = Services.Count(s => s.Status == ServiceStatus.Running);
        var stopped = Services.Count(s => s.Status == ServiceStatus.Stopped);
        var degraded = Services.Count(s => s.Status == ServiceStatus.Running && s.Health != ServiceHealth.Healthy);
        var deploymentPending = Services.Count(s => s.Status == ServiceStatus.DeploymentPending);
        var deploying = Services.Count(s => s.Status == ServiceStatus.Deploying);
        var unknown = Services.Count(s => s.Status == ServiceStatus.Unknown);

        return (total, running, stopped, degraded, deploymentPending, deploying, unknown);
    }
}