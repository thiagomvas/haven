using Haven.Domain.Events;

namespace Haven.Domain.Aggregates;

public sealed class Project : AggregateRoot
{
    public string Name { get; private set; }
    public string? Description { get; private set; }

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
            Raise(new ProjectUpdatedEvent(this));
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