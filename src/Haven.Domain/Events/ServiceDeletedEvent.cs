namespace Haven.Domain.Events;

public sealed record ServiceDeletedEvent(Guid ServiceId, string Name) : DomainEvent
{
    public override string ToMessage() => $"Service \"{Name}\" ({ServiceId}) was deleted";
}
