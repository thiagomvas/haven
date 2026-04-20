using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IEventRepository
{
    Task<PagedResult<Event>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? eventType,
        DateTime? from,
        DateTime? to,
        bool ascending,
        CancellationToken cancellationToken);
}
