using Haven.Domain.Entities;
using Haven.Domain.Events.User;

namespace Haven.Domain.Aggregates;

public class User : AggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool RequirePasswordChange { get; set; } = false;
    public bool IsAdmin { get; set; } = false;

    private readonly List<UserPermission> _permissions = [];
    public IReadOnlyCollection<UserPermission> Permissions => _permissions.AsReadOnly();

    public const int MaxNameLength = 64;

    private User() { }

    public static User Create(string name, string email, string passwordHash, bool isAdmin = false)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            RequirePasswordChange = false,
            IsAdmin = isAdmin,
        };

        user.Raise(new UserCreatedEvent(user.Id, user.Name));

        return user;
    }

    public static User CreatePending(string name, string email, string temporaryPasswordHash)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = temporaryPasswordHash,
            RequirePasswordChange = true,
            IsAdmin = false,
        };

        user.Raise(new UserCreatedEvent(user.Id, user.Name));

        return user;
    }

    public void GrantPermission(string permission)
    {
        if (_permissions.Any(p => p.Name == permission))
            return;
        _permissions.Add(UserPermission.For(Id, permission));
    }

    public void RevokePermission(string permission)
    {
        var entry = _permissions.FirstOrDefault(p => p.Name == permission);
        if (entry is not null)
            _permissions.Remove(entry);
    }

    public bool HasPermission(string permission) =>
        IsAdmin || _permissions.Any(p => p.Name == permission);
}