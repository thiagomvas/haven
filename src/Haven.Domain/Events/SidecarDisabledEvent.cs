using Haven.Domain.Enums;

namespace Haven.Domain.Events;

public sealed record SidecarDisabledEvent(Guid SidecarId, string Name) : SidecarLifetimeDomainEvent(SidecarId, Name), IScopedDomainEvent
{
    public NotificationScope PrimaryScope => NotificationScope.Sidecar;
    public Guid PrimaryScopeId => SidecarId;
    public override string ToMessage() => $"Sidecar \"{Name}\" ({SidecarId}) was disabled";
}