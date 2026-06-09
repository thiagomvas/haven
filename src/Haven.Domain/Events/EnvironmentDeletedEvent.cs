namespace Haven.Domain.Events;

public sealed record EnvironmentDeletedEvent(Guid EnvironmentId, string Name) : DomainEvent
{
    public override string ToMessage() => $"Environment \"{Name}\" ({EnvironmentId}) was deleted";
}