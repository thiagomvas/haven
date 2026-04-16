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
        var environment = _environments.Find(e => e.Id == environmentId)
            ?? throw new NotFoundException($"Environment '{environmentId}' not found in project '{Name}'.");

        var (hasChanges, oldName) = environment.Update(name, description);
        if (hasChanges)
            Raise(new Events.EnvironmentUpdatedEvent(this, environment, oldName));
    }

    public void RemoveEnvironment(Guid environmentId)
    {
        var environment = _environments.Find(e => e.Id == environmentId)
            ?? throw new NotFoundException($"Environment '{environmentId}' not found in project '{Name}'.");

        _environments.Remove(environment);
        Raise(new Events.EnvironmentDeletedEvent(this, environment));
    }

    public static Project Reconstitute(Guid id, string name, string? description)
    {
        return new Project
        {
            Id = id,
            Name = name,
            Description = description
        };
    }
}