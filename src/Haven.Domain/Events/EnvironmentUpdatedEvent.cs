using Haven.Domain.Aggregates;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Domain.Events;

public sealed record EnvironmentUpdatedEvent(Project Project, Environment Environment, string OldName) : DomainEvent
{
    public override string ToMessage() => $"\"{Environment.Name}\" environment was updated in \"{Project.Name}\" (previously \"{OldName}\")";
}
