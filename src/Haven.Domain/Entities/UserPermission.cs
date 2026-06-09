namespace Haven.Domain.Entities;

public sealed class UserPermission
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private UserPermission() { }

    internal static UserPermission For(Guid userId, string name) => new() { UserId = userId, Name = name };
}