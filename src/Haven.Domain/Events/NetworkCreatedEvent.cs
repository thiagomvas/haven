using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Domain.Events;

public sealed record NetworkCreatedEvent(Guid NetworkId, string Name) : DomainEvent, IScopedDomainEvent
{
    public NotificationScope PrimaryScope => NotificationScope.Global;
    public Guid PrimaryScopeId => NetworkId;
    public override string ToMessage() => $"Network \"{Name}\" ({NetworkId}) was created";
}