namespace Haven.Domain.Events;

public sealed record ServiceStoppedEvent(Guid ServiceId, string Name) : DomainEvent
{
    public override string ToMessage() => $"Service \"{Name}\" ({ServiceId}) was stopped";
}
