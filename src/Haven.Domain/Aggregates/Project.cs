using Haven.Domain.Entities;
using Haven.Domain.Exceptions;
using Haven.Domain.Events;
using Haven.Domain.Models;
using Haven.Domain.ValueObjects;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Domain.Aggregates;

/// <summary>
/// Represents a software system managed by Haven.
/// The top level of the three-level hierarchy: Project → Environment → Service.
/// </summary>
public sealed class Project : AggregateRoot, ISoftDeletable
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
    /// Timestamp when this project was soft-deleted. Null if not deleted.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// The deployment contexts (dev, staging, prod, etc.) that belong to this project.
    /// Never accessed in isolation, always navigated to through the owning Project.
    /// Exposed as read-only because all mutations must go through the aggregate root.
    /// </summary>
    public IReadOnlyList<Environment> Environments => _environments.AsReadOnly();

    private List<Environment> _environments = [];

    public const int MinNameLength = 2;
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

        result.Raise(new ProjectCreatedEvent(result.Id, result.Name));
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
            Raise(new ProjectUpdatedEvent(Id, oldName, Name));
    }

    public void Delete()
    {
        foreach (var environment in Environments)
        {
            environment.Delete();
        }
        
        Raise(new ProjectDeletedEvent(Id, Name));
    }


    public Environment AddEnvironment(string name, string? description = null)
    {
        var environment = Environment.Create(Id, name, description);
        _environments.Add(environment);
        environment.Project = this;
        return environment;
    }

    public void UpdateEnvironment(Guid environmentId, Optional<string> name = default, Optional<string?> description = default)
    {
        var environment = GetEnvironment(environmentId);

        environment.Update(name, description);
    }

    public void RemoveEnvironment(Guid environmentId)
    {
        var environment = GetEnvironment(environmentId);
        _environments.Remove(environment);

        environment.Delete();
    }

    public Service AddService(Guid environmentId, string name, ServiceType type, ExposureMode exposureMode, ServiceSourceConfig? sourceConfig = null)
    {
        var environment = GetEnvironment(environmentId);
        var service = environment.AddService(name, type, exposureMode, sourceConfig);
        return service;
    }

    public void UpdateService(Guid environmentId, Guid serviceId, Optional<string> name = default, Optional<ServiceType> type = default, Optional<ExposureMode> exposureMode = default, Optional<ServiceSourceConfig?> sourceConfig = default)
    {
        var environment = GetEnvironment(environmentId);
        environment.UpdateService(serviceId, name, type, exposureMode, sourceConfig);
    }

    public void RemoveService(Guid environmentId, Guid serviceId)
    {
        var environment = GetEnvironment(environmentId);
        var service = environment.RemoveService(serviceId);
    }

    public void MarkServiceDeploymentPending(Guid environmentId, Guid serviceId)
    {
        var environment = GetEnvironment(environmentId);
        environment.MarkServiceDeploymentPending(serviceId);
    }

    public void DeployService(Guid environmentId, Guid serviceId)
    {
        var environment = GetEnvironment(environmentId);
        environment.DeployService(serviceId);
    }

    public void StopService(Guid environmentId, Guid serviceId)
    {
        var environment = GetEnvironment(environmentId);
        environment.StopService(serviceId);
    }

    public void RestartService(Guid environmentId, Guid serviceId)
    {
        var environment = GetEnvironment(environmentId);
        environment.RestartService(serviceId);
    }

    public void DegradeService(Guid environmentId, Guid serviceId)
    {
        var environment = GetEnvironment(environmentId);
        environment.DegradeService(serviceId);
    }

    private Environment GetEnvironment(Guid environmentId) =>
        _environments.Find(e => e.Id == environmentId)
            ?? throw new NotFoundException($"Environment '{environmentId}' not found in project '{Name}'.");

    public void UpdateEnvironmentVariables()
    {
        Raise(new EnvironmentVariablesUpdatedEvent(Id, EnvironmentVariableParentType.Project));
    }

    public static Project Reconstitute(Guid id, string name, string? description, IEnumerable<EnvironmentData>? environments = null)
    {
        var project = new Project
        {
            Id = id,
            Name = name,
            Description = description,
        };

        var reconstructedEnvironments = environments?
            .Select(e =>
            {
                var reconstructedServices = e.Services?
                    .Select(s =>
                    {
                        var service = Service.Reconstitute(
                            s.Id, s.EnvironmentId, s.Name, s.Type, s.ExposureMode, s.Status, s.CreatedAt, s.UpdatedAt,
                            s.SourceConfig);
                        if (!string.IsNullOrEmpty(s.Token))
                            service.Token = s.Token;
                        return service;
                    })
                    .ToList();

                var environment = Environment.Reconstitute(
                    e.Id,
                    e.ProjectId,
                    e.Name,
                    e.Description,
                    e.NetworkName,
                    reconstructedServices,
                    project);

                if (reconstructedServices != null)
                {
                    foreach (var service in reconstructedServices)
                    {
                        service.Environment = environment;
                    }
                }

                return environment;
            })
            .ToList() ?? [];

        project._environments = reconstructedEnvironments;
        return project;
    }
}
