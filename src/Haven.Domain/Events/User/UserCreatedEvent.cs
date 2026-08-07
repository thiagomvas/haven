namespace Haven.Domain.Events.User;

public record UserCreatedEvent(Guid Id, string Name, string Email) : DomainEvent
{
    public override string ToMessage()
    {
        return string.IsNullOrEmpty(Name) ? $"User invited ({Email})" : $"User {Name} created";
    }
}