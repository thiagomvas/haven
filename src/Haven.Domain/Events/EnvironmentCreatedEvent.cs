using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Mediator;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Domain.Events;

public sealed record EnvironmentCreatedEvent(Project Project, Environment Environment) : DomainEvent, IEntityCreatedEvent
{
    Entity IEntityCreatedEvent.CreatedEntity => Environment;

    public override string ToMessage() => $"\"{Environment.Name}\" environment was created in \"{Project.Name}\"";
}
