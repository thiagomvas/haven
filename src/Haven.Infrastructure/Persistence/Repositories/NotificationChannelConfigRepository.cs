using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Persistence.Extensions;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class NotificationChannelConfigRepository(HavenDbContext context) : INotificationChannelConfigRepository
{
    public Task<NotificationChannelConfig?> GetSystemDefaultAsync(NotificationChannel channel,
        CancellationToken cancellationToken)
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

    public Task<PagedResult<NotificationChannelConfig>> GetPagedAsync(int pageNumber, int pageSize,
        string? sortBy = null, string? search = null, bool sortAscending = true,
        CancellationToken cancellationToken = default)
    {
        var query = context.NotificationChannelConfigs
            .Include(c => c.NotificationRules)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(c => c.Name.ToLower().Contains(searchLower) ||
                                     c.Channel.ToString().ToLower().Contains(searchLower));
        }

        var sortedQuery = sortBy?.ToUpperInvariant() switch
        {
            "CHANNEL" => sortAscending
                ? query.OrderBy(c => c.Channel).ThenBy(c => c.Name)
                : query.OrderByDescending(c => c.Channel).ThenBy(c => c.Name),
            "ENABLED" => sortAscending
                ? query.OrderBy(c => c.Enabled).ThenBy(c => c.Name)
                : query.OrderByDescending(c => c.Enabled).ThenBy(c => c.Name),
            _ => sortAscending ? query.OrderBy(c => c.Name) : query.OrderByDescending(c => c.Name),
        };

        return sortedQuery.ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
    }

    public Task UpdateAsync(NotificationChannelConfig config, CancellationToken cancellationToken)
    {
        context.NotificationChannelConfigs.Update(config);
        return Task.CompletedTask;
    }

    public void Remove(NotificationChannelConfig config)
        => context.NotificationChannelConfigs.Remove(config);
}