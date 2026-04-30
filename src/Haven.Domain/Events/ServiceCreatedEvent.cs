namespace Haven.Domain.Events;

public sealed record ServiceCreatedEvent(Guid ServiceId, string Name) : DomainEvent
{
    public override string ToMessage() => $"Service \"{Name}\" ({ServiceId}) was created";
}
