using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface ISecretVariableRepository : IRepository
{
    Task<Guid> AddAsync(SecretVariable secret, CancellationToken cancellationToken);
    Task<SecretVariable?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsWithKeyForParentAsync(Guid parentId, EnvironmentVariableParentType parentType, string key, Guid excludeId, CancellationToken cancellationToken);
    Task<PagedResult<SecretVariable>> GetForParentPagedAsync(Guid parentId, EnvironmentVariableParentType parentType, int pageNumber, int pageSize, CancellationToken cancellationToken);
    void Remove(SecretVariable secret);
}
