using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IGitCredentialsRepository
{
    Task<Guid> AddAsync(GitCredentials credentials, CancellationToken cancellationToken);
    Task<GitCredentials?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<GitCredentials?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsWithDisplayNameAsync(string displayName, Guid excludeId, CancellationToken cancellationToken);
    Task<PagedResult<GitCredentials>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    void Remove(GitCredentials credentials);
    IAsyncEnumerable<GitCredentials> GetAsync(CancellationToken cancellationToken);
}
