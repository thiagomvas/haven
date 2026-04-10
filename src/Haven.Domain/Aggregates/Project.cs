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
}