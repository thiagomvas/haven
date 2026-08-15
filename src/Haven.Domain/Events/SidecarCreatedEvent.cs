using Haven.Domain.Enums;

namespace Haven.Domain.Events;

public sealed record SidecarCreatedEvent(Guid SidecarId, string Name) : DomainEvent, IScopedDomainEvent
{
    public NotificationScope PrimaryScope => NotificationScope.Sidecar;
    public Guid PrimaryScopeId => SidecarId;
    public override string ToMessage() => $"Sidecar \"{Name}\" ({SidecarId}) was created";
}
