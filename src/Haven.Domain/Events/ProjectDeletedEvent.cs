using Haven.Domain.Aggregates;
using Mediator;

namespace Haven.Domain.Events;

public sealed record ProjectDeletedEvent(Guid Id, string Name) : DomainEvent
{
    public override string ToMessage() => $"Project \"{Name}\" ({Id}) was deleted";
}
