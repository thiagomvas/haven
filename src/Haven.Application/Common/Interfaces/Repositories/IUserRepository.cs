using Haven.Domain.Aggregates;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
}
