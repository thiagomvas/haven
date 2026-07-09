using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface INotificationAttemptRepository
{
    Task<Guid> AddAsync(NotificationAttempt attempt, CancellationToken ct = default);
    Task<NotificationAttempt?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(NotificationAttempt attempt, CancellationToken ct = default);
    Task<PagedResult<NotificationAttempt>> GetPagedByChannelConfigIdAsync(Guid? channelConfigId, int pageNumber,
        int pageSize, CancellationToken ct = default);
}