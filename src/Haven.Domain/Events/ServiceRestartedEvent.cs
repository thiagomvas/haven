using Haven.Domain;

namespace Haven.Domain.Events;

public sealed record ServiceRestartedEvent(Guid ServiceId, string Name) : DomainEvent, IScopedDomainEvent
{
    public NotificationScope PrimaryScope => NotificationScope.Service;
    public Guid PrimaryScopeId => ServiceId;
    public override string ToMessage() => $"Service \"{Name}\" ({ServiceId}) was restarted";
}