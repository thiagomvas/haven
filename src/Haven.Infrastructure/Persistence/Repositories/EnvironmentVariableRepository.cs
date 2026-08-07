using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class EnvironmentVariableRepository(HavenDbContext db) : IEnvironmentVariableRepository
{
    public async Task<IEnumerable<EnvironmentVariables>> GetForServiceAsync(Guid serviceId,
        CancellationToken cancellationToken)
    {
        return await db.EnvironmentVariables
            .Where(x => x.ParentId == serviceId && x.ParentType == EnvironmentVariableParentType.Service)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<EnvironmentVariables>> GetForEnvironmentAsync(Guid environmentId,
        CancellationToken cancellationToken)
    {
        return await db.EnvironmentVariables
            .Where(x => x.ParentId == environmentId && x.ParentType == EnvironmentVariableParentType.Environment)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<EnvironmentVariables>> GetForProjectAsync(Guid projectId,
        CancellationToken cancellationToken)
    {
        return await db.EnvironmentVariables
            .Where(x => x.ParentId == projectId && x.ParentType == EnvironmentVariableParentType.Project)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(EnvironmentVariables environmentVariable, CancellationToken cancellationToken)
    {
        db.EnvironmentVariables.Add(environmentVariable);
        return Task.CompletedTask;
    }

    public Task AddAsync(IEnumerable<EnvironmentVariables> environmentVariables, CancellationToken cancellationToken)
    {
        db.EnvironmentVariables.AddRange(environmentVariables);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(EnvironmentVariables environmentVariable, CancellationToken cancellationToken)
    {
        db.EnvironmentVariables.Remove(environmentVariable);
        return Task.CompletedTask;
    }

    public Task CleanForProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        db.EnvironmentVariables.RemoveRange(db.EnvironmentVariables.Where(x =>
            x.ParentId == projectId && x.ParentType == EnvironmentVariableParentType.Project));
        return Task.CompletedTask;
    }

    public Task CleanForServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        db.EnvironmentVariables.RemoveRange(db.EnvironmentVariables.Where(x =>
            x.ParentId == serviceId && x.ParentType == EnvironmentVariableParentType.Service));
        return Task.CompletedTask;
    }

    public Task CleanForEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken)
    {
        db.EnvironmentVariables.RemoveRange(db.EnvironmentVariables.Where(x =>
            x.ParentId == environmentId && x.ParentType == EnvironmentVariableParentType.Environment));
        return Task.CompletedTask;
    }
}