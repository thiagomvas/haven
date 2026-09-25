using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Persistence.Extensions;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class SecretVariableRepository(HavenDbContext context) : ISecretVariableRepository
{
    public Task<Guid> AddAsync(SecretVariable secret, CancellationToken cancellationToken)
    {
        context.Secrets.Add(secret);
        return Task.FromResult(secret.Id);
    }

    public async Task<SecretVariable?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await context.Secrets.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> ExistsWithKeyForParentAsync(Guid parentId, EnvironmentVariableParentType parentType, string key, Guid excludeId, CancellationToken cancellationToken)
        => context.Secrets.AnyAsync(
            s => s.ParentId == parentId && s.ParentType == parentType && s.Key == key && s.Id != excludeId,
            cancellationToken);

    public Task<PagedResult<SecretVariable>> GetForParentPagedAsync(Guid parentId, EnvironmentVariableParentType parentType, int pageNumber, int pageSize, CancellationToken cancellationToken)
        => context.Secrets
            .Where(s => s.ParentId == parentId && s.ParentType == parentType)
            .OrderBy(s => s.Key)
            .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);

    public void Remove(SecretVariable secret)
        => context.Secrets.Remove(secret);
}
