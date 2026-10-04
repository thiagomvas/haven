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

    public async Task<IReadOnlyList<CustomAction>> GetForServiceAsync(Guid serviceId, CancellationToken ct)
    {
        return await context.CustomActions
            .Where(a => a.ServiceId == serviceId)
            .OrderBy(a => a.ActionName)
            .ToListAsync(ct);
    }
}
