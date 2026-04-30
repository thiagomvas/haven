using Haven.Domain.Aggregates;
using Mediator;

namespace Haven.Domain.Events;

public sealed record ProjectCreatedEvent(Guid Id, string Name) : DomainEvent
{
    public override string ToMessage() => $"Project \"{Name}\" ({Id}) was created";
}
