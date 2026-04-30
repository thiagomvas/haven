using Haven.Domain.Aggregates;

namespace Haven.Domain.Events;

public sealed record ProjectUpdatedEvent(Guid Id, string OldName, string? NewName = null) : DomainEvent
{
    public override string ToMessage()
    {
        if (string.IsNullOrWhiteSpace(NewName))
            return $"Project '{OldName}' ({Id}) was updated";
        return $"Project '{NewName}' ({Id}, formerly {OldName}) was updated";
    }
}
