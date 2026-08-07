using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface INotificationChannelConfigRepository
{
    Task<Guid> AddAsync(NotificationChannelConfig config, CancellationToken cancellationToken);
    Task<NotificationChannelConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<NotificationChannelConfig>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<NotificationChannelConfig?> GetSystemDefaultAsync(NotificationChannel channel, CancellationToken cancellationToken);
    Task UpdateAsync(NotificationChannelConfig config, CancellationToken cancellationToken);
    void Remove(NotificationChannelConfig config);
}