using Haven.Domain;

namespace Haven.Domain.Events;

public sealed record EnvironmentUpdatedEvent(Guid EnvironmentId, string OldName, string NewName) : DomainEvent, IScopedDomainEvent
{
    public NotificationScope PrimaryScope => NotificationScope.Environment;
    public Guid PrimaryScopeId => EnvironmentId;
    public override string ToMessage() => $"Environment \"{NewName}\" ({EnvironmentId}) was updated";
}