using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IUserInviteTokenRepository
{
    Task<Guid> AddAsync(UserInviteToken token, CancellationToken cancellationToken);
    Task<UserInviteToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task<List<UserInviteToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    void Remove(UserInviteToken token);
}