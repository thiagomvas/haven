namespace Haven.Domain.Events;

public sealed record ServiceDeployedEvent(Guid ServiceId, string Name) : DomainEvent
{
    public override string ToMessage() => $"Service \"{Name}\" ({ServiceId}) was deployed";
}
