using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class PermissionRepository(HavenDbContext context) : IPermissionRepository
{
    public Task<bool> UserHasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken)
        => context.Set<UserPermission>()
            .AnyAsync(p => p.UserId == userId && p.Name == permission, cancellationToken);
}
