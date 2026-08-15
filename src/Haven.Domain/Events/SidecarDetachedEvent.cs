using Haven.Domain.Enums;

namespace Haven.Domain.Events;

public sealed record SidecarDetachedEvent(Guid SidecarId, string Name, Guid NetworkId) : DomainEvent, IScopedDomainEvent
{
    public NotificationScope PrimaryScope => NotificationScope.Sidecar;
    public Guid PrimaryScopeId => SidecarId;
    public override string ToMessage() => $"Sidecar \"{Name}\" ({SidecarId}) was detached from network {NetworkId}";
}
