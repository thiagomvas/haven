namespace Haven.Domain.Events;

public sealed record EnvironmentCreatedEvent(Guid EnvironmentId, string Name) : DomainEvent
{
    public override string ToMessage() => $"Environment \"{Name}\" ({EnvironmentId}) was created";
}