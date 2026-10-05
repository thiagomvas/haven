using Haven.Application.Features.CustomActions.Abstractions;
using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public sealed class CustomActionRepository(HavenDbContext context) : ICustomActionRepository
{
    public async Task<CustomAction?> GetByIdAsync(Guid actionId, CancellationToken ct)
    {
        return await context.CustomActions.FirstOrDefaultAsync(a => a.Id == actionId, ct);
    }

    public async Task<CustomAction?> GetByTokenAsync(string token, CancellationToken ct)
    {
        return await context.CustomActions.FirstOrDefaultAsync(a => a.Token == token, ct);
    }

    public async Task<IReadOnlyList<CustomAction>> GetForServiceAsync(Guid serviceId, CancellationToken ct)
    {
        return await context.CustomActions
            .Where(a => a.ServiceId == serviceId)
            .OrderBy(a => a.ActionName)
            .ToListAsync(ct);
    }

    public Task AddAsync(CustomAction action, CancellationToken ct)
    {
        context.CustomActions.Add(action);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(CustomAction action, CancellationToken ct)
    {
        context.CustomActions.Remove(action);
        return Task.CompletedTask;
    }
}