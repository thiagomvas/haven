using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class NotificationRuleRepository(HavenDbContext context) : INotificationRuleRepository
{
    public async Task<Dictionary<string, int>> GetGlobalRuleCountsByEventTypeAsync(CancellationToken cancellationToken = default)
        => await context.NotificationRules
            .Where(r => r.Scope == NotificationScope.Global)
            .GroupBy(r => r.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventType, x => x.Count, cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetChannelIdsForEventAsync(string eventType, CancellationToken cancellationToken = default)
        => await context.NotificationRules
            .Where(r => r.EventType == eventType && r.Scope == NotificationScope.Global)
            .Select(r => r.ChannelConfigId)
            .ToListAsync(cancellationToken);

    public async Task SetGlobalRulesForEventAsync(string eventType, IEnumerable<Guid> channelIds, CancellationToken cancellationToken = default)
    {
        var existing = await context.NotificationRules
            .Where(r => r.EventType == eventType && r.Scope == NotificationScope.Global)
            .ToListAsync(cancellationToken);

        context.NotificationRules.RemoveRange(existing);

        var newRules = channelIds.Select(channelId => new NotificationRule
        {
            ChannelConfigId = channelId,
            EventType = eventType,
            Scope = NotificationScope.Global,
            Enabled = true,
        });

        context.NotificationRules.AddRange(newRules);
    }
}
