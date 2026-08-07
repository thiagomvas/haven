namespace Haven.Domain.Entities;

public class UserInviteToken : Entity
{
    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsAccepted => AcceptedAt.HasValue;
    public bool IsActive => !IsExpired && !IsRevoked && !IsAccepted;

    private UserInviteToken() { }

    public static UserInviteToken Create(Guid userId, string tokenHash, DateTime expiresAt) =>
        new()
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

    public void Revoke() => RevokedAt = DateTime.UtcNow;
    public void MarkAccepted() => AcceptedAt = DateTime.UtcNow;
}