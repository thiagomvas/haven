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

    public async Task<Dictionary<string, IReadOnlyList<Guid>>> GetAllGlobalRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = await context.NotificationRules
            .Where(r => r.Scope == NotificationScope.Global)
            .Select(r => new { r.EventType, r.ChannelConfigId })
            .ToListAsync(cancellationToken);

        return rules
            .GroupBy(r => r.EventType)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Guid>)g.Select(r => r.ChannelConfigId).ToList());
    }

    public async Task<IReadOnlyList<Guid>> GetChannelIdsForEventAsync(string eventType, CancellationToken cancellationToken = default)
        => await context.NotificationRules
            .Where(r => r.EventType == eventType && r.Scope == NotificationScope.Global)
            .Select(r => r.ChannelConfigId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NotificationRule>> GetEnabledRulesForEventAsync(string eventType, CancellationToken cancellationToken = default)
        => await context.NotificationRules
            .Include(r => r.ChannelConfig)
            .Where(r => r.EventType == eventType && r.Scope == NotificationScope.Global && r.Enabled && r.ChannelConfig!.Enabled)
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

    public async Task<Dictionary<string, int>> GetScopedRuleCountsByEventTypeAsync(NotificationScope scope, Guid scopeId, CancellationToken cancellationToken = default)
        => await context.NotificationRules
            .Where(r => r.Scope == scope && r.ScopeId == scopeId)
            .GroupBy(r => r.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventType, x => x.Count, cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetChannelIdsForScopedEventAsync(string eventType, NotificationScope scope, Guid scopeId, CancellationToken cancellationToken = default)
        => await context.NotificationRules
            .Where(r => r.EventType == eventType && r.Scope == scope && r.ScopeId == scopeId)
            .Select(r => r.ChannelConfigId)
            .ToListAsync(cancellationToken);

    public async Task<Dictionary<string, IReadOnlyList<Guid>>> GetAllScopedRulesAsync(NotificationScope scope, Guid scopeId, CancellationToken cancellationToken = default)
    {
        var rules = await context.NotificationRules
            .Where(r => r.Scope == scope && r.ScopeId == scopeId)
            .Select(r => new { r.EventType, r.ChannelConfigId })
            .ToListAsync(cancellationToken);

        return rules
            .GroupBy(r => r.EventType)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Guid>)g.Select(r => r.ChannelConfigId).ToList());
    }

    public async Task<bool> HasAnyScopedRulesAsync(NotificationScope scope, Guid scopeId, CancellationToken cancellationToken = default)
        => await context.NotificationRules
            .AnyAsync(r => r.Scope == scope && r.ScopeId == scopeId, cancellationToken);

    public async Task<IReadOnlyList<NotificationRule>> GetEnabledScopedRulesForEventAsync(string eventType, NotificationScope scope, Guid scopeId, CancellationToken cancellationToken = default)
        => await context.NotificationRules
            .Include(r => r.ChannelConfig)
            .Where(r => r.EventType == eventType && r.Scope == scope && r.ScopeId == scopeId && r.Enabled && r.ChannelConfig!.Enabled)
            .ToListAsync(cancellationToken);

    public async Task SetScopedRulesForEventAsync(string eventType, NotificationScope scope, Guid scopeId, IEnumerable<Guid> channelIds, CancellationToken cancellationToken = default)
    {
        var existing = await context.NotificationRules
            .Where(r => r.EventType == eventType && r.Scope == scope && r.ScopeId == scopeId)
            .ToListAsync(cancellationToken);

        context.NotificationRules.RemoveRange(existing);

        var newRules = channelIds.Select(channelId => new NotificationRule
        {
            ChannelConfigId = channelId,
            EventType = eventType,
            Scope = scope,
            ScopeId = scopeId,
            Enabled = true,
        });

        context.NotificationRules.AddRange(newRules);
    }

    public async Task ClearScopedRulesForEventAsync(string eventType, NotificationScope scope, Guid scopeId, CancellationToken cancellationToken = default)
    {
        var existing = await context.NotificationRules
            .Where(r => r.EventType == eventType && r.Scope == scope && r.ScopeId == scopeId)
            .ToListAsync(cancellationToken);

        context.NotificationRules.RemoveRange(existing);
    }
}