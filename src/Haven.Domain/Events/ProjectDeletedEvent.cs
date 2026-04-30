namespace Haven.Domain.Events;

public sealed record ProjectDeletedEvent(Guid ProjectId, string Name) : DomainEvent
{
    public override string ToMessage() => $"Project \"{Name}\" ({ProjectId}) was deleted";
}
