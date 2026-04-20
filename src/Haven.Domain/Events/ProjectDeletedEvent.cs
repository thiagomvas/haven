using Haven.Domain.Aggregates;
using Mediator;

namespace Haven.Domain.Events;

public sealed record ProjectDeletedEvent(Project Project) : DomainEvent
{
    public override string ToMessage() => $"\"{Project.Name}\" project was deleted";
}
