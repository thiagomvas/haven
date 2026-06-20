using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class PermissionRepository(HavenDbContext context) : IPermissionRepository
{
    public async Task<bool> UserHasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken)
    {
        var isAdmin = await context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.IsAdmin)
            .FirstOrDefaultAsync(cancellationToken);

        if (isAdmin)
            return true;

        return await context.Set<UserPermission>()
            .AnyAsync(p => p.UserId == userId && p.Name == permission, cancellationToken);
    }
}