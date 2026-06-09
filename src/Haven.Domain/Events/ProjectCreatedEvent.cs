namespace Haven.Domain.Events;

public sealed record ProjectCreatedEvent(Guid ProjectId, string Name) : DomainEvent
{
    public override string ToMessage() => $"Project \"{Name}\" ({ProjectId}) was created";
}