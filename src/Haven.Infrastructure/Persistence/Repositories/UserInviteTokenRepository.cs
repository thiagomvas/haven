using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class UserInviteTokenRepository(HavenDbContext context) : IUserInviteTokenRepository
{
    public Task<Guid> AddAsync(UserInviteToken token, CancellationToken cancellationToken)
    {
        context.UserInviteTokens.Add(token);
        return Task.FromResult(token.Id);
    }

    public Task<UserInviteToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
        => context.UserInviteTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public Task<List<UserInviteToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        => context.UserInviteTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.AcceptedAt == null)
            .ToListAsync(cancellationToken);

    public void Remove(UserInviteToken token) => context.UserInviteTokens.Remove(token);
}
