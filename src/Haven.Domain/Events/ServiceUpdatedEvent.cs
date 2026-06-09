namespace Haven.Domain.Events;

public sealed record ServiceUpdatedEvent(Guid ServiceId, string OldName, string NewName) : DomainEvent
{
    public override string ToMessage() => $"Service \"{NewName}\" ({ServiceId}) was updated";
}