using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public sealed class HavenSettingRepository(HavenDbContext context) : IHavenSettingRepository
{
    public async Task<string?> GetAsync(string category, CancellationToken ct)
    {
        var setting = await context.Settings
            .FirstOrDefaultAsync(s => s.Category == category, ct);
        return setting?.Value;
    }

    public async Task UpsertAsync(string category, string value, CancellationToken ct)
    {
        var existing = await context.Settings
            .FirstOrDefaultAsync(s => s.Category == category, ct);

        if (existing is null)
        {
            var newSetting = HavenSetting.Create(category, value);
            context.Settings.Add(newSetting);
        }
        else
        {
            existing.Update(value);
        }
    }
}