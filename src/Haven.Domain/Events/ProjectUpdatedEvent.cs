using Haven.Domain;

namespace Haven.Domain.Events;

public sealed record ProjectUpdatedEvent(Guid ProjectId, string OldName, string NewName) : DomainEvent, IScopedDomainEvent
{
    public NotificationScope PrimaryScope => NotificationScope.Project;
    public Guid PrimaryScopeId => ProjectId;
    public override string ToMessage() => $"Project \"{NewName}\" ({ProjectId}) was updated";
}