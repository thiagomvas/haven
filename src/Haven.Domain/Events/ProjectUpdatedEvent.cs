using Haven.Domain.Aggregates;

namespace Haven.Domain.Events;

public sealed record ProjectUpdatedEvent(Project Project, string OldName) : DomainEvent
{
    public override string ToMessage() => $"\"{Project.Name}\" project was updated (previously \"{OldName}\")";
}
