namespace Haven.Domain.Events;

public sealed record ProjectUpdatedEvent(Guid ProjectId, string OldName, string NewName) : DomainEvent
{
    public override string ToMessage() => $"Project \"{NewName}\" ({ProjectId}) was updated";
}