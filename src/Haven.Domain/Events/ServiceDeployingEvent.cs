namespace Haven.Domain.Events;

public sealed record ServiceDeployingEvent(Guid ServiceId, string Name) : DomainEvent
{
    public override string ToMessage()
    {
        return $"Deploying service '{Name}'";
    }
}