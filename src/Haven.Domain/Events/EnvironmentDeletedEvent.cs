using Haven.Domain.Aggregates;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Domain.Events;

public sealed record EnvironmentDeletedEvent(Project Project, Environment Environment) : DomainEvent
{
    public override string ToMessage() => $"\"{Environment.Name}\" environment was deleted from \"{Project.Name}\"";
}
