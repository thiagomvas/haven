namespace Haven.Domain.Events;

public sealed record ServiceRestartedEvent(Guid ServiceId, string Name) : DomainEvent
{
    public override string ToMessage() => $"Service \"{Name}\" ({ServiceId}) was restarted";
}
