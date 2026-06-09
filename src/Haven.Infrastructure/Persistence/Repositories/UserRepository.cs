using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class UserRepository(HavenDbContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
        => context.Users
            .Include(u => u.Permissions)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
        => context.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public Task<Guid> AddAsync(User user, CancellationToken cancellationToken)
    {
        context.Users.Add(user);
        return Task.FromResult(user.Id);
    }

    public Task<List<User>> GetAllAsync(CancellationToken cancellationToken)
        => context.Users
            .Include(u => u.Permissions)
            .ToListAsync(cancellationToken);

    public void Remove(User user) => context.Users.Remove(user);
}