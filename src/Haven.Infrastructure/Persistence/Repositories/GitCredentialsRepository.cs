using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;
using Haven.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class GitCredentialsRepository(HavenDbContext context) : IGitCredentialsRepository
{
    public Task<Guid> AddAsync(GitCredentials credentials, CancellationToken cancellationToken)
    {
        context.GitCredentials.Add(credentials);
        return Task.FromResult(credentials.Id);
    }

    public async Task<GitCredentials?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await context.GitCredentials.FirstOrDefaultAsync(gc => gc.Id == id, cancellationToken);

    public async Task<GitCredentials?> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return await context.Services
            .Where(s => s.Id == serviceId)
            .Select(s => s.GitCredentials)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<GitCredentials?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => await context.GitCredentials.FindAsync([id], cancellationToken);

    public Task<bool> ExistsWithDisplayNameAsync(string displayName, Guid excludeId, CancellationToken cancellationToken)
        => context.GitCredentials.AnyAsync(
            gc => gc.DisplayName == displayName && gc.Id != excludeId,
            cancellationToken);

    public Task<PagedResult<GitCredentials>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
        => context.GitCredentials
            .OrderBy(gc => gc.DisplayName)
            .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);

    public void Remove(GitCredentials credentials)
        => context.GitCredentials.Remove(credentials);

    public IAsyncEnumerable<GitCredentials> GetAsync(CancellationToken cancellationToken)
        => context.GitCredentials.AsAsyncEnumerable();
}
