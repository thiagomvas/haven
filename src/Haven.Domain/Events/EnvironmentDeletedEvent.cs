using Haven.Domain;

namespace Haven.Domain.Events;

public sealed record EnvironmentDeletedEvent(Guid EnvironmentId, string Name) : DomainEvent, IScopedDomainEvent
{
    public NotificationScope PrimaryScope => NotificationScope.Environment;
    public Guid PrimaryScopeId => EnvironmentId;
    public override string ToMessage() => $"Environment \"{Name}\" ({EnvironmentId}) was deleted";
}
