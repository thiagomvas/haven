using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Domain.Events;

public sealed record ServiceUpdatedEvent(Guid ServiceId, string OldName, string NewName) : DomainEvent, IScopedDomainEvent
{
    public NotificationScope PrimaryScope => NotificationScope.Service;
    public Guid PrimaryScopeId => ServiceId;
    public override string ToMessage() => $"Service \"{NewName}\" ({ServiceId}) was updated";
}