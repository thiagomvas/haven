using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class UserRepository(HavenDbContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
        => context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
}
