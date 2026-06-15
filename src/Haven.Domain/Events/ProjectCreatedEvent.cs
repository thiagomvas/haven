using Haven.Domain;

namespace Haven.Domain.Events;

public sealed record ProjectCreatedEvent(Guid ProjectId, string Name) : DomainEvent, IScopedDomainEvent
{
    public NotificationScope PrimaryScope => NotificationScope.Project;
    public Guid PrimaryScopeId => ProjectId;
    public override string ToMessage() => $"Project \"{Name}\" ({ProjectId}) was created";
}
