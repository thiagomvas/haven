namespace Haven.Application.Common.Interfaces.Repositories;

public interface INotificationRuleRepository
{
    Task<Dictionary<string, int>> GetGlobalRuleCountsByEventTypeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetChannelIdsForEventAsync(string eventType, CancellationToken cancellationToken = default);
    Task SetGlobalRulesForEventAsync(string eventType, IEnumerable<Guid> channelIds, CancellationToken cancellationToken = default);
}
