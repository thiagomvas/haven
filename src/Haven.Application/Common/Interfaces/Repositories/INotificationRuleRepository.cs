using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface INotificationRuleRepository
{
    Task<Dictionary<string, int>> GetGlobalRuleCountsByEventTypeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetChannelIdsForEventAsync(string eventType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationRule>> GetEnabledRulesForEventAsync(string eventType, CancellationToken cancellationToken = default);
    Task<Dictionary<string, IReadOnlyList<Guid>>> GetAllGlobalRulesAsync(CancellationToken cancellationToken = default);
    Task SetGlobalRulesForEventAsync(string eventType, IEnumerable<Guid> channelIds, CancellationToken cancellationToken = default);

    Task<Dictionary<string, int>> GetScopedRuleCountsByEventTypeAsync(NotificationScope scope, Guid scopeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetChannelIdsForScopedEventAsync(string eventType, NotificationScope scope, Guid scopeId, CancellationToken cancellationToken = default);
    Task<Dictionary<string, IReadOnlyList<Guid>>> GetAllScopedRulesAsync(NotificationScope scope, Guid scopeId, CancellationToken cancellationToken = default);
    Task<bool> HasAnyScopedRulesAsync(NotificationScope scope, Guid scopeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationRule>> GetEnabledScopedRulesForEventAsync(string eventType, NotificationScope scope, Guid scopeId, CancellationToken cancellationToken = default);
    Task SetScopedRulesForEventAsync(string eventType, NotificationScope scope, Guid scopeId, IEnumerable<Guid> channelIds, CancellationToken cancellationToken = default);
    Task ClearScopedRulesForEventAsync(string eventType, NotificationScope scope, Guid scopeId, CancellationToken cancellationToken = default);
}