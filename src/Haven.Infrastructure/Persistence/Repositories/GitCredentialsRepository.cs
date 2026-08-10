using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;
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
        var service = await context.Services
            .Include(s => s.GitCredentials)
            .FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);

        if (service is null)
            return null;

        if (service.GitCredentials is not null)
            return service.GitCredentials;

        // GitCredentialId is backfilled from the source config on create/update, but services persisted
        // before that existed may still have it unset. Fall back to reading it out of the source config
        // directly so those services don't lose access to their configured credential.
        var fallbackCredentialId = (service.SourceConfig as DockerfileConfig)?.GitCredentialId;
        if (fallbackCredentialId is null)
            return null;

        return await context.GitCredentials.FirstOrDefaultAsync(gc => gc.Id == fallbackCredentialId, cancellationToken);
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