namespace Haven.Domain.Events.User;

public record UserCreatedEvent(Guid Id, string Name) : DomainEvent
{
    public override string ToMessage()
    {
        return $"User {Name} created";
    }
}