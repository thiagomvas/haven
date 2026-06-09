namespace Haven.Domain.Events;

public sealed record EnvironmentUpdatedEvent(Guid EnvironmentId, string OldName, string NewName) : DomainEvent
{
    public override string ToMessage() => $"Environment \"{NewName}\" ({EnvironmentId}) was updated";
}