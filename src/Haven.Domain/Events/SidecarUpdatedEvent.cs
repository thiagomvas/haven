using Haven.Domain.Enums;

namespace Haven.Domain.Events;

public sealed record SidecarUpdatedEvent(Guid SidecarId, string OldName, string NewName) : DomainEvent, IScopedDomainEvent
{
    public NotificationScope PrimaryScope => NotificationScope.Sidecar;
    public Guid PrimaryScopeId => SidecarId;
    public override string ToMessage() => $"Sidecar \"{NewName}\" ({SidecarId}) was updated";
}