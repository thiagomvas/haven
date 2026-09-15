namespace Haven.Application.Common.Interfaces.Repositories;

public interface IPermissionRepository : IRepository
{
    Task<bool> UserHasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken);
}