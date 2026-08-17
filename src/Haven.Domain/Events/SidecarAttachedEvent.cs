using Haven.Domain.Enums;

namespace Haven.Domain.Events;

public sealed record SidecarAttachedEvent(Guid SidecarId, string Name, Guid NetworkId) : DomainEvent, IScopedDomainEvent
{
    public NotificationScope PrimaryScope => NotificationScope.Sidecar;
    public Guid PrimaryScopeId => SidecarId;
    public override string ToMessage() => $"Sidecar \"{Name}\" ({SidecarId}) was attached to network {NetworkId}";
}