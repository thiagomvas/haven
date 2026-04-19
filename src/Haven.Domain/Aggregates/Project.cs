using Haven.Domain.Entities;
using Haven.Domain.Exceptions;
using Haven.Domain.Events;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Domain.Aggregates;

/// <summary>
/// Represents a software system managed by Haven.
/// The top level of the three-level hierarchy: Project → Environment → Service.
/// </summary>
public sealed class Project : AggregateRoot
{
    /// <summary>
    /// The human-readable identifier for this project.
    /// Unique across the entire Haven instance because it forms part of the DNS namespace,
    /// a project named "my-app" means all its services are resolvable under *.my-app.haven.
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Optional free-text description of the project.
    /// No functional role, purely informational for the dashboard.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// The deployment contexts (dev, staging, prod, etc.) that belong to this project.
    /// Never accessed in isolation, always navigated to through the owning Project.
    /// Exposed as read-only because all mutations must go through the aggregate root.
    /// </summary>
    public IReadOnlyList<Environment> Environments => _environments.AsReadOnly();

    private List<Environment> _environments = [];

    public const int MaxNameLength = 64;
    public const int MaxDescriptionLength = 250;

    private Project()
    {
    }

    public static Project Create(string name, string? description = null)
    {
        var result = new Project()
        {
            Name = name,
            Description = description
        };

        result.Raise(new Events.ProjectCreatedEvent(result));
        return result;
    }

    public void Update(Optional<string> name = default, Optional<string?> description = default)
    {
        var oldName = Name;
        bool hasChanges = false;

        if (name.HasValue && name.Value != Name)
        {
            Name = name.Value;
            hasChanges = true;
        }

        if (description.HasValue && description.Value != Description)
        {
            Description = description.Value;
            hasChanges = true;
        }

        if (hasChanges)
            Raise(new ProjectUpdatedEvent(this, oldName));
    }

    public void Delete() => Raise(new ProjectDeletedEvent(this));

    public Environment AddEnvironment(string name, string? description = null)
    {
        var environment = Environment.Create(Id, name, description);
        _environments.Add(environment);
        Raise(new Events.EnvironmentCreatedEvent(this, environment));
        return environment;
    }

    public void UpdateEnvironment(Guid environmentId, Optional<string> name = default, Optional<string?> description = default)
    {
        var environment = GetEnvironment(environmentId);

        var (hasChanges, oldName) = environment.Update(name, description);
        if (hasChanges)
            Raise(new Events.EnvironmentUpdatedEvent(this, environment, oldName));
    }

    public void RemoveEnvironment(Guid environmentId)
    {
        var environment = GetEnvironment(environmentId);
        _environments.Remove(environment);
        Raise(new Events.EnvironmentDeletedEvent(this, environment));
    }

    public Service AddService(Guid environmentId, string name, ServiceType type, ExposureMode exposureMode)
    {
        var environment = GetEnvironment(environmentId);
        var service = environment.AddService(name, type, exposureMode);
        Raise(new ServiceCreatedEvent(this, environment, service));
        return service;
    }

    public void UpdateService(Guid environmentId, Guid serviceId, Optional<string> name = default, Optional<ServiceType> type = default, Optional<ExposureMode> exposureMode = default)
    {
        var environment = GetEnvironment(environmentId);
        var hasChanges = environment.UpdateService(serviceId, name, type, exposureMode);

        if (hasChanges)
        {
            var service = environment.Services.First(s => s.Id == serviceId);
            Raise(new ServiceUpdatedEvent(this, environment, service));
        }
    }

    public void RemoveService(Guid environmentId, Guid serviceId)
    {
        var environment = GetEnvironment(environmentId);
        var service = environment.RemoveService(serviceId);
        Raise(new ServiceDeletedEvent(this, environment, service));
    }

    public void DeployService(Guid environmentId, Guid serviceId)
    {
        var environment = GetEnvironment(environmentId);
        environment.DeployService(serviceId);
        var service = environment.Services.First(s => s.Id == serviceId);
        Raise(new ServiceDeployedEvent(this, environment, service));
    }

    public void StopService(Guid environmentId, Guid serviceId)
    {
        var environment = GetEnvironment(environmentId);
        environment.StopService(serviceId);
        var service = environment.Services.First(s => s.Id == serviceId);
        Raise(new ServiceStoppedEvent(this, environment, service));
    }

    private Environment GetEnvironment(Guid environmentId) =>
        _environments.Find(e => e.Id == environmentId)
            ?? throw new NotFoundException($"Environment '{environmentId}' not found in project '{Name}'.");

    public sealed record ServiceData(Guid Id, Guid EnvironmentId, string Name, ServiceType Type, ExposureMode ExposureMode, ServiceStatus Status, DateTime CreatedAt, DateTime UpdatedAt);
    public sealed record EnvironmentData(Guid Id, Guid ProjectId, string Name, string? Description, string NetworkName, IEnumerable<ServiceData>? Services = null);

    public static Project Reconstitute(Guid id, string name, string? description, IEnumerable<EnvironmentData>? environments = null)
    {
        return new Project
        {
            Id = id,
            Name = name,
            Description = description,
            _environments = environments?
                .Select(e => Environment.Reconstitute(
                    e.Id,
                    e.ProjectId,
                    e.Name,
                    e.Description,
                    e.NetworkName,
                    e.Services?.Select(s => Service.Reconstitute(
                        s.Id, s.EnvironmentId, s.Name, s.Type, s.ExposureMode, s.Status, s.CreatedAt, s.UpdatedAt))))
                .ToList() ?? []
        };
    }
}
