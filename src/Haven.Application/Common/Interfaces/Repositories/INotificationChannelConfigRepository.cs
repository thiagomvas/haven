using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface INotificationChannelConfigRepository
{
    Task<Guid> AddAsync(NotificationChannelConfig config, CancellationToken cancellationToken);
    Task<NotificationChannelConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<NotificationChannelConfig>> GetPagedAsync(int pageNumber, int pageSize,
        string? sortBy = null, string? search = null, bool sortAscending = true,
        CancellationToken cancellationToken = default);
    Task<NotificationChannelConfig?> GetSystemDefaultAsync(NotificationChannel channel, CancellationToken cancellationToken);
    Task UpdateAsync(NotificationChannelConfig config, CancellationToken cancellationToken);
    void Remove(NotificationChannelConfig config);
}