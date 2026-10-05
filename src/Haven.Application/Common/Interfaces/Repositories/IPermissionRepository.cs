using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IPermissionRepository : IRepository
{
    Task<bool> UserHasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken);
    Task<IEnumerable<UserPermission>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken);
}