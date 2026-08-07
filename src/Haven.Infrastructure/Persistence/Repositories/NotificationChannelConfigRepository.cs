using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Infrastructure.Persistence.Extensions;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class NotificationChannelConfigRepository(HavenDbContext context) : INotificationChannelConfigRepository
{
    public Task<NotificationChannelConfig?> GetSystemDefaultAsync(NotificationChannel channel, CancellationToken cancellationToken)
        => context.NotificationChannelConfigs
            .FirstOrDefaultAsync(c => c.Channel == channel && c.IsSystemDefault, cancellationToken);

    public Task<Guid> AddAsync(NotificationChannelConfig config, CancellationToken cancellationToken)
    {
        context.NotificationChannelConfigs.Add(config);
        return Task.FromResult(config.Id);
    }

    public async Task<NotificationChannelConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await context.NotificationChannelConfigs
            .Include(c => c.NotificationRules)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<PagedResult<NotificationChannelConfig>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
        => context.NotificationChannelConfigs
            .Include(c => c.NotificationRules)
            .OrderByDescending(c => c.Enabled).ThenBy(c => c.Name)
            .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);

    public Task UpdateAsync(NotificationChannelConfig config, CancellationToken cancellationToken)
    {
        context.NotificationChannelConfigs.Update(config);
        return Task.CompletedTask;
    }

    public void Remove(NotificationChannelConfig config)
        => context.NotificationChannelConfigs.Remove(config);
}