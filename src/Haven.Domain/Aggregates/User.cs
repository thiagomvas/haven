using Haven.Domain.Events.User;

namespace Haven.Domain.Aggregates;

public class User : AggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool RequirePasswordChange { get; set; } = false;
    
    public const int MaxNameLength = 64;
    
    private User() {}

    public static User Create(string name, string email, string passwordHash)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            RequirePasswordChange = false,
        };

        user.Raise(new UserCreatedEvent(user.Id, user.Name));
        
        return user;
    }
}